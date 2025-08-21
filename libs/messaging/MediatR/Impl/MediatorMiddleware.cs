
namespace Sencilla.Messaging.Mediator;

/// <summary>
/// Process commands in memory 
/// Inject resolvable as TEvent generic param is used in ProcessAsync 
/// </summary>
public class MediatorMiddleware(IResolver resolver/*, MediatrConfig config*/) : Resolveable(resolver), IMessageMiddleware
{
    public async Task ProcessAsync<T>(Message<T>? message, CancellationToken cancellationToken = default)
    {
        // process all handlers for the message first 
        var handlers = All<IMessageHandler<Message<T>>>();
        foreach (var handler in handlers)
        {
            if (handler is not null)
                await handler.HandleAsync(message);

            DoneMessage(message);
        }

        // process all handlers for the message type
        var typeHandlers = All<IMessageHandler<T>>();
        foreach (var typeHandler in typeHandlers)
        {
            if (typeHandler is null) continue;
            if (message is null) continue;

            await typeHandler.HandleAsync(message.Payload);

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
