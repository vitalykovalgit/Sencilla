
namespace Sencilla.Messaging;

/// <summary>
/// Process commands in memory 
/// Inject resolvable as TEvent generic param is used in ProcessAsync 
/// </summary>
public class InMemoryMiddleware(IResolver resolver) : Resolveable(resolver), IMessageMiddleware
{
    public async Task ProcessAsync<TMessage>(TMessage message) where TMessage : class
    {
        var handlers = All<IMessageHandler<TMessage>>();
        foreach (var handler in handlers)
            await handler.HandleAsync(message);
    }
}
