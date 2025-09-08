
namespace Sencilla.Messaging.Mediator;

/// <summary>
/// Process commands in memory 
/// Inject resolvable as TEvent generic param is used in ProcessAsync 
/// </summary>
public class MediatorMiddleware(IServiceProvider provider/*, MediatrConfig config*/) : IMessageMiddleware
{
    public async Task ProcessAsync<T>(Message<T>? message, CancellationToken cancellationToken = default)
    {
        // process all handlers for the message first 
        var handlers = provider.GetServices<IMessageHandler<Message<T>>>();
        foreach (var handler in handlers)
        {
            if (handler is not null && message is not null)
                await handler.HandleAsync(message, cancellationToken);

            DoneMessage(message);
        }

        // process all handlers for the message type
        var typeHandlers = provider.GetServices<IMessageHandler<T>>();
        foreach (var typeHandler in typeHandlers)
        {
            if (typeHandler is null) continue;
            if (message is null) continue;
            if (message.Payload is null) continue;

            await typeHandler.HandleAsync(message.Payload, cancellationToken);

            DoneMessage(message);
        }
    }

    private static void DoneMessage<T>(Message<T>? message)
    {
        if (message is null) return;

        // Mark the message as processed
        message.ProcessedAt = DateTime.UtcNow;
    }
}
