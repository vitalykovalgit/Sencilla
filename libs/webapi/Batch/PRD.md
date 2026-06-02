---
title: Sencilla Batch API
status: draft
version: v1
owner: platform
created: 2026-06-02
---

# Sencilla Batch API — PRD

## 1. Problem

Multi-step operations on the server (create an order + its items + shipping +
payment; delete a project + its files + items + orders) currently require a
bespoke backend command/handler per workflow. Each new workflow means new C#
code, a new endpoint, and a deploy.

We want the **frontend to compose a multi-step operation** from existing CRUD
primitives and submit it as a single request. The server executes the steps in
order, wires the output of one step into the input of the next, and optionally
wraps the whole thing in a database transaction.

> Highly complex or performance-critical workflows can still get a dedicated
> endpoint. The batch API is the default for the long tail of dependent CRUD
> work, so we stop writing a handler for every combination.

## 2. Goals / Non-goals

**Goals (v1)**

- One endpoint — `POST /api/v1/batch` — that runs an ordered list of CRUD/GET steps.
- Output-to-input wiring via temporary keys (`$ref:step.field`).
- Opt-in single-transaction execution (all-or-nothing).
- Per-step structured result, including which step failed and why.
- Idempotency for retried (background-job) batches.
- A typed TypeScript `BatchBuilder` so the frontend doesn't hand-write JSON.
- Lives in Sencilla, reusable by any consumer of the repository pattern.

**Non-goals (v1) — explicitly deferred**

- Conditional/branching steps (`run C only if B succeeded`).
- Custom `ICommandHandler`/`IMessageHandler` steps (CRUD primitives only).
- GET results as substitution sources (GETs are return-to-client only).
- Array iteration / fan-out over a `getAll` result.
- Per-step authorization model (see [[#9 Security]] — separate session).
- Replacing `[CrudApi]` with `[SencillaEntity]` (they coexist for now).
- Cross-`DbContext` transactions.

## 3. Workflows

**W1 — delete a project (cascade).** Strict linear chain; FK constraints force
the parent last. The client sends the correct order; the server does not reorder
writes.

```
delete files → delete/modify orders → delete project items → delete project
```

**W2 — create an order (fan-out).** The order is created first; items, shipping,
and payment all reference the new order id.

```
create order
  ├─ create order items   (orderId = $ref:order.id)
  ├─ create shipping      (orderId = $ref:order.id)
  └─ create base payment  (orderId = $ref:order.id)
```

**W3 — batched lookups.** Replace N GET round-trips with one request that returns
several lookup tables (`getAll Country`, `getAll Currency`, …).

## 4. Request format

```json
{
  "version": "v1",
  "transactional": true,
  "idempotencyKey": "job-xyz-123",
  "steps": [
    { "ref": "countries", "op": "getAll",  "entity": "Country", "filter": { "take": 100, "orderBy": ["name"] } },
    { "ref": "order",     "op": "create",  "entity": "Order",     "body": { "customerId": 5, "status": "pending" } },
    { "ref": "item",      "op": "create",  "entity": "OrderItem", "body": { "orderId": "$ref:order.id", "productId": 12, "qty": 2 } },
    { "ref": "shipping",  "op": "create",  "entity": "Shipping",  "body": { "orderId": "$ref:order.id", "address": "..." } }
  ]
}
```

| Field | Meaning |
| --- | --- |
| `version` | Schema version. `v1` only; other values rejected. |
| `transactional` | `true` (default) → one DB transaction over all write steps. `false` → no transaction (fire-and-forget). |
| `idempotencyKey` | Optional. Required by convention for background jobs. Same key returns the cached result without re-executing. |
| `steps[]` | Ordered. Max **50**. |
| `step.ref` | Unique handle used by `$ref` and in the response. |
| `step.op` | One of the ops in [[#5 Operations]]. |
| `step.entity` | The `[SencillaEntity]` name (not the CLR type, not the route). |
| `step.body` | Entity payload for write ops. |
| `step.filter` | `IFilter` shape for `getAll` / `getCount`. |
| `step.id` | Key for `getById` and id-based `delete`. |

### Key substitution

A **top-level** string body value of the form `$ref:<stepRef>.<field>` is replaced
with `<field>` from the result of `<stepRef>` before the step runs.

- v1 substitutes **top-level entity fields only** (no nested objects).
- The referenced step must be an **earlier write step**. Referencing a later step,
  a GET step, or a missing step is a validation error. This rule makes dependency
  cycles structurally impossible (a back-reference can never resolve).

## 5. Operations

| `op` | Repository | Tx? | Result `data` |
| --- | --- | --- | --- |
| `create` | `ICreateRepository.Create` | yes | full created entity (status 201) |
| `update` | `IUpdateRepository.Update` | yes | updated entity |
| `upsert` | `ICreateRepository.UpsertAsync` (key = `Id`) | yes | entity |
| `delete` | `IDeleteRepository.Delete` | yes | affected count |
| `remove` | `IRemoveRepository.Remove` (soft delete) | yes | entity |
| `hide` / `show` | `IHideRepository.Hide`/`Show` | yes | entity |
| `getAll` | `IReadRepository.GetAll` | no | entity array |
| `getById` | `IReadRepository.GetById` | no | entity or `null` |
| `getCount` | `IReadRepository.GetCount` | no | number |

An op the target entity doesn't support (no matching repository registered, or
not allowed by its `[SencillaEntity]` flags) is rejected in pre-flight.

## 6. Execution model

1. **Validate** (pre-flight, before any DB work): version, step count ≤ 50,
   unique refs, known entities, op allowed per entity, every `$ref` points to an
   earlier write step. Any failure → `400` with `validationErrors`, nothing runs.
2. **Idempotency**: if `idempotencyKey` is present and cached, return the cached
   result immediately.
3. **GET steps run first**, outside the transaction, regardless of their position
   in the payload. They never participate in substitution, so order among them is
   irrelevant. A `getById` miss yields `data: null`, status `200` (not an abort).
4. **Open one transaction** (if `transactional`) on the shared `DbContext`.
5. **Write steps run in declared order.** For each: resolve `$ref` values →
   deserialize → call the repository → store the returned entity under `ref`.
6. **On a write failure**: map the exception to a status, record the error, mark
   all remaining writes `skipped`, **roll back**, return `success: false`,
   `rolledBack: true`.
7. **On success**: commit. Assemble step results in the **original declared order**.
8. **Persist** the result under `idempotencyKey` (TTL 24h) if provided.

> **Why GETs first:** keeps the write transaction window as short as possible —
> a read never holds the transaction open.

## 7. Response format

```json
{
  "success": true,
  "rolledBack": false,
  "steps": [
    { "ref": "countries", "op": "getAll", "entity": "Country",   "status": 200, "data": [ /* ... */ ] },
    { "ref": "order",     "op": "create", "entity": "Order",     "status": 201, "data": { "id": 42, "customerId": 5 } },
    { "ref": "item",      "op": "create", "entity": "OrderItem", "status": 201, "data": { "id": 7, "orderId": 42 } },
    { "ref": "shipping",  "op": "create", "entity": "Shipping",  "status": 201, "data": { "id": 3, "orderId": 42 } }
  ]
}
```

Failure (write step 3 fails, transactional):

```json
{
  "success": false,
  "rolledBack": true,
  "steps": [
    { "ref": "countries", "op": "getAll", "status": 200, "data": [ /* ... */ ] },
    { "ref": "order",     "op": "create", "status": 201, "data": { "id": 42 } },
    { "ref": "item",      "op": "create", "status": 404, "error": "Entity 'Order' id 42 not found" },
    { "ref": "shipping",  "op": "create", "status": 0,   "skipped": true }
  ]
}
```

- Pre-flight failures → HTTP `400`, body carries `validationErrors`.
- Execution outcomes (including a rolled-back batch) → HTTP `200`; the per-step
  `status` and top-level `success` tell the client what happened.
- Per-step status mapping mirrors `ApiController`: `UnauthorizedException` → 401,
  `ForbiddenException` → 403, otherwise 500; create success → 201, else 200.

## 8. Entity opt-in

An entity is batchable only if it carries `[SencillaEntity]` and enables the op.
This is the whitelist anchor (also the future home for auth scoping).

```csharp
[CrudApi("api/v1/orders")]
[SencillaEntity("Order", Batch = BatchOpFlags.All)]
public class Order : IEntity, IEntityCreateable, IEntityUpdateable, IEntityDeleteable
{
    public int Id { get; set; }
    public int CustomerId { get; set; }
    public string Status { get; set; } = "";
}
```

`[SencillaEntity]` and `[CrudApi]` coexist in v1. `[SencillaEntity]` is intended
to eventually become the single registration point (name + API + batch + caching).

## 9. Security

Deferred to a dedicated session. v1 ships with the batch endpoint under the host's
default authentication; **there is no per-step authorization yet**. The intended
model: the client presents a token; each step is validated against the caller's
permissions (per-step or whole-batch). `[SencillaEntity]` is the planned anchor for
this. Until then, treat batch as equivalent in trust to direct CRUD endpoints and
do not expose sensitive entities via `[SencillaEntity]`.

## 10. Idempotency

`IBatchIdempotencyStore` maps `idempotencyKey → BatchResult` with a 24h TTL. Default
implementation is `IMemoryCache`-backed (single-instance). For multi-pod deploys,
register a distributed (Redis) implementation — the host already has Redis via
Aspire. Background jobs MUST send an `idempotencyKey` so a crash-retry that
partially committed under `transactional:false`, or any duplicate delivery, returns
the original result instead of re-running.

## 11. Constraints & limits

- Max **50** steps per batch (configurable via `BatchOptions.MaxSteps`).
- Transactional batches assume **all participating entities share one `DbContext`**
  (true for Photoboost — all entities use `DynamicDbContext`). Mixed-context
  batches are out of scope; document and reject later if needed.
- Substitution: top-level string fields only.
- GET results are not substitutable.

## 12. Observability

Each step logs a structured entry: `batchId`, `stepRef`, `entity`, `op`,
`status`, `durationMs`. The batch logs totals and the rollback decision. Uses the
existing `ILogger` pipeline.

## 13. Package & surface

New package **`Sencilla.Web.Batch`** (`libs/webapi/Batch`), references
`Sencilla.Web`.

- Wire contracts: `BatchRequest`, `BatchStep`, `BatchResult`, `BatchStepResult`.
- `SencillaEntityAttribute`, `BatchOpFlags`, `BatchOp`.
- `IBatchExecutor` / `BatchExecutor` — pipeline.
- `IBatchEntityRegistry` / `BatchEntityRegistry` — startup scan of `[SencillaEntity]`.
- `IBatchEntityInvoker` / `BatchEntityInvoker<TEntity,TKey>` — typed repo dispatch
  (one instance per entity; the only reflection is `MakeGenericType` at startup).
- `BatchRefResolver` — `$ref` substitution. `BatchValidator` — pre-flight checks.
- `IBatchIdempotencyStore` / `MemoryCacheBatchIdempotencyStore`.
- `BatchController` — `POST /api/v1/batch`.
- `AddSencillaBatch(IMvcBuilder, Action<BatchOptions>?)` — registers services and
  the controller's application part.

Host wiring (one line next to `AddSencillaWeb`):

```csharp
var mvc = services.AddControllers();
services.AddSencillaWeb(mvc);
services.AddSencillaBatch(mvc);
```

## 14. TypeScript client

`BatchBuilder` produces the payload and type-checks `$ref` field names against the
entity type. Writes return a ref handle; GETs return void (not substitutable).

```ts
const b = new BatchBuilder()
const order = b.create(Order, { customerId: 5, status: 'pending' })
b.create(OrderItem, { orderId: order.ref('id'), productId: 12, qty: 2 })
b.create(Shipping,  { orderId: order.ref('id'), address: '...' })
b.getAll(Country,   { take: 100 })
const result = await b.execute({ transactional: true })
```

## 15. Open questions / future

- Promote `[SencillaEntity]` to subsume `[CrudApi]` (name + api + cache + batch).
- Per-step / whole-batch authorization.
- GET (`getById`) results as substitution sources.
- Custom command steps via a `BatchCommandRegistry`.
- Distributed Redis idempotency store as a shipped add-on.
