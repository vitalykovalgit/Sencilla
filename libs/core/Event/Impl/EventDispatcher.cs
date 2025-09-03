
namespace Sencilla.Core;

public class EventDispatcher(IEnumerable<IEventMiddleware> middlewares) : IEventDispatcher
{
    public async Task PublishAsync<T>(T @event, CancellationToken token) where T : class, IEvent
    {
        foreach (var m in middlewares)
            await m.ProcessAsync(@event, token);
    }
}
