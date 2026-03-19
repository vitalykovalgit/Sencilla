namespace Sencilla.Messaging;

/// <summary>
/// Resolves and invokes IMessageHandler&lt;Message&lt;T&gt;&gt; and IMessageHandler&lt;T&gt; from a scoped provider.
/// Sets ProcessedAt on successful execution.
/// </summary>
public class MessageHandlerExecutor : IMessageHandlerExecutor
{
    public async Task ExecuteAsync<T>(Message<T> message, IServiceProvider scopedProvider, CancellationToken cancellationToken = default)
    {
        var handlers = scopedProvider.GetServices<IMessageHandler<Message<T>>>();
        foreach (var handler in handlers)
        {
            await handler.HandleAsync(message, cancellationToken);
            message.ProcessedAt = DateTime.UtcNow;
        }

        if (message.Payload is null)
            return;

        var payloadHandlers = scopedProvider.GetServices<IMessageHandler<T>>();
        foreach (var handler in payloadHandlers)
        {
            await handler.HandleAsync(message.Payload, cancellationToken);
            message.ProcessedAt = DateTime.UtcNow;
        }
    }
}
