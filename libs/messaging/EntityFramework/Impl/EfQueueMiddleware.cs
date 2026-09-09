namespace Sencilla.Messaging.EntityFramework;

/// <summary>
/// Routes a message whose <c>[Stream]</c> or route names a queue this provider declares into
/// [Message], through the caller's scoped <see cref="IMessageQueue"/> — which is why this
/// middleware is scoped: the row commits with whatever transaction the caller has open, so
/// <c>dapp.Send(...)</c> inside an endpoint's or a handler's transaction is an outbox write.
///
/// Like every transport middleware it then passes the message on. Whether an in-process Mediator
/// also runs it is that Mediator's decision — it skips <c>[Stream]</c> types unless configured with
/// <c>HandleDurable()</c> — so registration order does not matter.
/// </summary>
[DisableInjection]
public class EfQueueMiddleware(EfMessagingProviderConfig config, IMessageQueue queue) : MessageMiddleware, IMessageMiddleware
{
    public async Task HandleAsync<T>(Message<T> message, Func<Message<T>, CancellationToken, Task> next, CancellationToken cancellationToken = default)
    {
        // One row per stream shares the envelope id — routing one message to two durable queues
        // is not a supported shape, and the primary key says so loudly if it is tried.
        foreach (var stream in GetStreamNames<T>(config))
            await queue.Enqueue(message, stream, null, cancellationToken);

        await next(message, cancellationToken);
    }
}
