# Plan: Refactor Sencilla Messaging Core, Mediator, InMemoryQueue

**TL;DR:** Refactor three messaging libraries to adopt the chain-of-responsibility middleware pattern, complete the `MessageStreamConsumer` with real handler dispatch, add `MediatorConfig` filtering, fix all coding-standard violations, extend tests, and update docs.

---

## Phase 1 — Core Library (`libs/messaging/Core/`)

**Step 1.1 — Rename folders** (no code changes, just filesystem renames + `.csproj` glob updates)
- `Attribute/` → `Attributes/`
- `Contract/` → `Contracts/`
- `Entity/` → `Entities/`

**Step 1.2 — Refactor `IMessageMiddleware`** (`Contracts/IMessageMiddleware.cs`)
- Rename `ProcessAsync` → `HandleAsync` and add `next` delegate:
  `Task HandleAsync<T>(Message<T> message, Func<Message<T>, CancellationToken, Task> next, CancellationToken token = default)`

**Step 1.3 — Refactor `MessageDispatcher`** (`Impl/MessageDispatcher.cs`)
- Add `ConcurrentDictionary<Type, object> PipelineCache`
- On first `Send<T>`, build the chain right-to-left from `Middlewares[]`, box as `Func<Message<T>, CancellationToken, Task>`, cache by type
- Subsequent calls for same T unbox and invoke — no per-call loop rebuilding
- Terminal step: `(_, _) => Task.CompletedTask`

**Step 1.4 — Fix `MessageMiddleware` base class** (`Impl/MessageMiddleware.cs`)
- Rename `_streamCache` → `StreamCache`

**Step 1.5 — Extract `IMessageHandlerExecutor` + `MessageHandlerExecutor`** (NEW files)
- `IMessageHandlerExecutor`: `Task ExecuteAsync<T>(Message<T>, IServiceProvider scopedProvider, CancellationToken)`
- Impl resolves `IMessageHandler<Message<T>>` then `IMessageHandler<T>`, invokes each, sets `ProcessedAt`
- Registered as `AddSingleton` in `Bootstrap.cs` — *shared by MediatorMiddleware and MessageStreamConsumer*

**Step 1.6 — Complete `MessageStreamConsumer`** (`Impl/MessageStreamConsumer.cs`) — *depends on 1.5*
- Inject `IServiceScopeFactory` and `IMessageHandlerExecutor`
- After reading raw JSON: deserialize to base `Message` → read `Namespace` → `Type.GetType(Namespace)`
- Unknown type → log warning + skip (no throw)
- Deserialize `Message<T>` via `JsonSerializer.Deserialize(json, typeof(Message<T>))`
- Create DI scope → call `executor.ExecuteAsync<T>(message, scope.ServiceProvider, token)`
- Concurrency: wrap each message in `SemaphoreSlim`-guarded task when `MaxConcurrentHandlers > 1`
- Remove all `Console.WriteLine`; use `ILogger<MessageStreamConsumer>` (now required, not optional)

**Step 1.7 — `Bootstrap.cs` updates**
- Add `System.Text.Json`, `System.Reflection` to global usings if absent
- Register `IMessageHandlerExecutor` → `MessageHandlerExecutor` as singleton

---

## Phase 2 — Mediator Library (`libs/messaging/Mediator/`) — *parallel with Phase 3*

**Step 2.1 — Fix filename typo**: rename `Boostrap.cs` → `Bootstrap.cs`

**Step 2.2 — Add filter API to `MediatorConfig`** (rename `Entity/` folder → `Entities/`)
- Private `HashSet<Type> AllowedTypes`, `HashSet<Type> DisabledTypes`, `bool AllowAllFlag = true`
- Public: `AllowAll()`, `DisableAll()`, `Allow<T>()`/`Allow(Type)`, `Disable<T>()`/`Disable(Type)`
- Public `bool ShouldHandle(Type t)` used by middleware

**Step 2.3 — Update `MediatorMiddleware`** (*depends on 1.5*)
- Implement `HandleAsync<T>(Message<T>, Func<...> next, CancellationToken)`
- Check `config.ShouldHandle(typeof(T))` first; if false → skip to `await next(message, token)`
- Delegate handler invocation to injected `IMessageHandlerExecutor`
- Always call `await next(message, token)` at the end (chain continues)

---

## Phase 3 — InMemoryQueue Library (`libs/messaging/InMemoryQueue/`) — *parallel with Phase 2*

**Step 3.1 — Update `InMemoryQueueMiddleware`** (`Impl/InMemoryQueueMiddleware.cs`)
- Implement `HandleAsync<T>(Message<T>, Func<...> next, CancellationToken)`
- Write to stream, then call `await next(message, token)`

**Step 3.2 — Fix `InMemoryStreamProvider.GetStream` semantics**
- `GetStream(config)` → `Streams.TryGetValue()` → return null if not found; do NOT create
- `GetOrCreateStream` keeps existing create-if-absent behavior

---

## Phase 4 — Tests — *depends on Phases 1–3*

**Step 4.1 — Update existing tests** (all stub middlewares use old `ProcessAsync` signature)
- `tests/messaging/Core/MessageDispatcherTests.cs`
- `tests/messaging/Core/MessageDispatcherEdgeCaseTests.cs`
- `tests/messaging/Mediator/MediatorMiddlewareTests.cs`
- `tests/messaging/InMemoryQueue/InMemoryQueueMiddlewareTests.cs`

**Step 4.2 — New chain-pattern tests** (extend `MessageDispatcherTests.cs`)
- Pre/post `next` execution order verified
- Short-circuit: middleware omitting `next` stops chain
- Exception before `next` / after `next` propagation
- Pipeline built once per type T

**Step 4.3 — New `MessageHandlerExecutorTests.cs`** (NEW)
- Both `IMessageHandler<Message<T>>` and `IMessageHandler<T>` invoked
- `ProcessedAt` stamp set after execution
- No handlers → no exception; scoped resolution works

**Step 4.4 — New `MessageStreamConsumerTests.cs`** (NEW)
- Handler called for known type; unknown `Namespace` → warning + no throw
- `MaxConcurrentHandlers` limits parallel reads via SemaphoreSlim
- Cancellation stops loop gracefully

**Step 4.5 — New MediatorConfig filter tests** (extend `MediatorMiddlewareTests.cs`)
- `DisableAll` → handlers not called, `next` called
- `AllowAll` (default) → handlers called
- `Disable<T>` → only that type skipped
- `Allow<T>` after `DisableAll` → only that type handled

**Step 4.6 — `InMemoryStreamProviderTests.cs`** — add test that `GetStream` returns null for missing stream

---

## Phase 5 — Documentation

**Step 5.1** — `docs/messaging/README.md`
- Update `IMessageMiddleware` interface block to chain signature
- Update retry middleware example
- Document `MessageHandlerExecutor` and stream→handler flow

**Step 5.2** — `docs/messaging/Architecture.md`
- Add stream consumer dispatch diagram
- Document `MediatorConfig` filtering API

---

## Relevant Files

| File | Change |
|---|---|
| `libs/messaging/Core/Contracts/IMessageMiddleware.cs` | Chain `HandleAsync` signature |
| `libs/messaging/Core/Contracts/IMessageHandlerExecutor.cs` | **NEW** |
| `libs/messaging/Core/Impl/MessageDispatcher.cs` | Pipeline cache |
| `libs/messaging/Core/Impl/MessageMiddleware.cs` | Rename `StreamCache` |
| `libs/messaging/Core/Impl/MessageStreamConsumer.cs` | Full implementation |
| `libs/messaging/Core/Impl/MessageHandlerExecutor.cs` | **NEW** |
| `libs/messaging/Core/Bootstrap.cs` | Register executor; global usings |
| `libs/messaging/Mediator/Bootstrap.cs` | Rename from `Boostrap.cs` |
| `libs/messaging/Mediator/Entities/MediatorConfig.cs` | Filter API |
| `libs/messaging/Mediator/Impl/MediatorMiddleware.cs` | Chain `+` executor `+` filter |
| `libs/messaging/InMemoryQueue/Impl/InMemoryQueueMiddleware.cs` | Chain signature |
| `libs/messaging/InMemoryQueue/Impl/InMemoryStreamProvider.cs` | Fix `GetStream` semantics |
| `tests/messaging/Core/MessageStreamConsumerTests.cs` | **NEW** |
| `tests/messaging/Core/MessageHandlerExecutorTests.cs` | **NEW** |
| All existing middleware test files | Update to chain signature |

---

## Verification

1. `dotnet build Sencilla.sln` — zero errors
2. `dotnet test` — all existing + new tests pass
3. Confirm `IMessageMiddleware` chain: a middleware that does work _after_ calling `next` executes correctly
4. Confirm `InMemoryStreamProvider.GetStream` returns `null` for missing streams
5. Confirm `MediatorConfig.Disable<T>()` prevents handler invocation

---

## Scope

**In scope:** Core, Mediator, InMemoryQueue — libraries, tests, and `docs/messaging/`
**Out of scope:** RabbitMQ, Kafka, ServiceBus, Redis, SignalR, EntityFramework, Scheduler providers (unchanged)
