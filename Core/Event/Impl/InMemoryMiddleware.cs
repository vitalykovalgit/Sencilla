
namespace Sencilla.Core
{

    /// <summary>
    /// Process commands in memory 
    /// Inject resolvable as TEvent generic param is used in ProcessAsync 
    /// </summary>
    [Implement(typeof(IEventMiddleware))]
    public class InMemoryMiddleware: Resolveable, IEventMiddleware
    {
        
        public InMemoryMiddleware(IResolver resolver): base(resolver)
        {
        }

        public async Task ProcessAsync<TEvent>(TEvent @event) where TEvent : class, IEvent
        {
            var handlers = All<IEventHandler<TEvent>>();
            foreach (var handler in handlers)
            {
                await handler.HandleAsync(@event);
            }
        }
    }
}
