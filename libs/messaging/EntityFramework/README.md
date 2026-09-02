# Sencilla.Messaging.EntityFramework

The database as a durable messaging transport. Messages become rows in `[Message]`; workers claim
them, run the registered handler, and acknowledge the outcome — so a message survives a worker
crash, retries with backoff, and ends in a state an operator can query.

Use it when the work must not be lost: background jobs, long-running operations, anything whose
failure has to be visible afterwards. For fire-and-forget notifications between processes, a broker
transport (RabbitMQ) is lighter.

## Enqueueing is transactional, and deliberately not the dispatcher

```csharp
using var tx = await projects.BeginTransaction(token);

var project = await projects.Create(new Project { ... }, token);
await queue.Enqueue(new Message<CloneProject> { EntityId = project.Id, UserId = user.Id, Payload = new(...) }, "tasks", token: token);

await tx.CommitAsync(token);   // row and message commit together, or neither does
```

`IMessageQueue` is **scoped**: it writes through the caller's `DbContext`, so the message lands in
the caller's transaction. `IMessageDispatcher` cannot do this — it is a singleton and cannot reach
the caller's scope — which is why durable enqueue has its own API. The distinction is the point:
one call is transactional, the other is not, and you can see which is which at the call site.

The `[Message]` table carries no user grants, so callers outside a request identity wrap the
enqueue in `Access.Root()` — their own endpoint or worker is the authorization.

## Handling

An ordinary handler, autoscanned like any other:

```csharp
public class CloneProjectHandler(...) : IMessageHandler<Message<CloneProject>>
{
    public async Task HandleAsync(Message<CloneProject> message, CancellationToken token) { ... }
}
```

Return normally and the message is acked. Throw and it is re-queued with backoff until
`MaxAttempts`, then marked `Failed`. Terminal failures raise `MessageFailed<T>`, so app-side
surfacing (flip a status, write a notification) is just another handler:

```csharp
public class CloneProjectFailedHandler(...) : IMessageHandler<MessageFailed<CloneProject>> { ... }
```

**Delivery is at-least-once.** A crash between a handler finishing and the ack re-runs it, so
handlers must be idempotent.

## Wiring

```csharp
services.AddSencillaMessaging(o =>
{
    o.UseMediator();
    o.UseEntityFramework(ef =>
    {
        ef.WithOptions(configuration);            // binds the "Messaging" section
        ef.AddStreams(s => s.AddQueue("tasks"));
        ef.AddConsumers(c => c.ForStream("tasks", _ => _.MaxConcurrentHandlers = 1));
    });
});
```

A host that only *enqueues* (a web app) declares the streams but no consumers. A worker declares
consumers too.

## Recovery and instance identity

`Messaging:InstanceId` **must be unique per process** — it defaults to the machine/pod name, which
is already unique per pod under Kubernetes. Two replicas sharing an id means one re-queues rows the
other is actively processing.

Recovery has two layers:

- **On start**, an instance re-queues anything left `InProgress` under its own name. A fresh process
  owns nothing in flight, so this is immediate and needs no claim cache.
- **By age**, rows `InProgress` longer than `StuckAfterMinutes` are re-queued regardless of owner —
  the instance that never comes back is exactly the case owner-scoped recovery cannot handle. Keep
  the threshold above the longest legitimate handler runtime.

## Options (`Messaging` section)

| Option | Default | |
| --- | --- | --- |
| `InstanceId` | machine name | claim identity; unique per process |
| `PollIntervalSeconds` | 20 | sleep when the stream is empty |
| `MaxAttempts` | 3 | attempts before `Failed` |
| `RetryBaseDelaySeconds` / `UseExponentialBackoff` | 2 / true | retry spacing |
| `StuckAfterMinutes` | 30 | age-based rescue threshold |
| `RetentionDays` / `RetentionSweepMinutes` | 90 / 60 | terminal-row cleanup; 0 disables |

## Schema

`Sencilla.Messaging.EntityFramework.Mssql` ships the `[Message]` table into your dacpac. No seed
data and no lookup tables — adding a message type touches no SQL at all.
