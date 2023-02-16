
namespace Sencilla.Core;

public class EventDispatcher : IEventDispatcher
{
    private readonly IEnumerable<IEventMiddleware> Middlewares;

    public EventDispatcher(IEnumerable<IEventMiddleware> middlewares)
    {
        Middlewares = middlewares;
    }

    public async Task PublishAsync<T>(T @event) where T : class, IEvent
    {
        foreach (var m in Middlewares)
            await m.ProcessAsync(@event);
    }
}
