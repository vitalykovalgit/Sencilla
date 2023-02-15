
namespace Sencilla.Core;

/// <summary>
/// Process commands in memory 
/// Inject resolvable as TEvent generic param is used in ProcessAsync 
/// </summary>
public class InMemoryMiddleware: Resolveable, IEventMiddleware
{
    
    public InMemoryMiddleware(IResolver resolver): base(resolver)
    {
    }

    public async Task ProcessAsync<TEvent>(TEvent @event) where TEvent : class, IEvent
    {
        var handlers = All<IEventHandlerBase<TEvent>>();
        foreach (var handler in handlers)
        {
            await handler.CallWithInjectAsync(nameof(ICommandHandler<ICommand>.HandleAsync), @event);
            //await handler.HandleAsync(@event);
        }
    }
}
