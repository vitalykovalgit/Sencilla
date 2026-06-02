# Sencilla.Web.Batch

One endpoint — `POST /api/v1/batch` — that runs an ordered list of dependent
CRUD/GET steps composed on the frontend, so multi-step server workflows don't each
need a bespoke command/handler.

- Output-to-input wiring via `$ref:step.field` (top-level write fields, v1).
- Opt-in single-transaction execution (all-or-nothing).
- GET steps run first, outside the transaction; writes in declared order.
- Structured per-step results; idempotency for retried batches.
- Entities opt in with `[SencillaEntity("Name", Batch = BatchOpFlags.All)]`.

See [PRD.md](PRD.md) for the full design.

## Wiring

```csharp
var mvc = services.AddControllers();
services.AddSencillaWeb(mvc);
services.AddSencillaBatch(mvc);
```

## Entity opt-in

```csharp
[CrudApi("api/v1/orders")]
[SencillaEntity("Order", Batch = BatchOpFlags.All)]
public class Order : IEntity, IEntityCreateable, IEntityUpdateable, IEntityDeleteable { }
```
