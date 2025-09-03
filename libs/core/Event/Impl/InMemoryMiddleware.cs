namespace Sencilla.Core;

/// <summary>
/// Process commands in memory 
/// Inject resolvable as TEvent generic param is used in ProcessAsync 
/// </summary>
public class InMemoryMiddleware(IServiceProvider provider): IEventMiddleware
{
    public async Task ProcessAsync<TEvent>(TEvent @event, CancellationToken token) where TEvent : class, IEvent
    {
        //var handlers = provider.GetServices<IEventHandler<TEvent>>();
        //foreach (var handler in handlers)
        //    await handler.HandleAsync(@event, token);

        // Resolve base handlers 
        var baseHandlers = provider.GetServices<IEventHandlerBase<TEvent>>();
        foreach (var handler in baseHandlers)
            await provider.InvokeMethod(handler, nameof(ICommandHandler<ICommand>.HandleAsync), @event, token);
            //await handler.CallWithInjectAsync(nameof(ICommandHandler<ICommand>.HandleAsync), @event);
    }
}
