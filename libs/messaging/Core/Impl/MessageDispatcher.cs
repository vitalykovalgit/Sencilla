
namespace Sencilla.Messaging;

public class EventDispatcher(IEnumerable<IMessageMiddleware> middlewares) : IMessageDispatcher
{
    public async Task Publish<T>(T message) where T : class
    {
        foreach (var m in middlewares)
            await m.ProcessAsync(message);

    }
}
