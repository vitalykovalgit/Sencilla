
namespace Sencilla.Messaging;

[SingletonLifetime]
public class MessageDispatcher(IServiceProvider provider, MessagingConfig config) : IMessageDispatcher
{
    /// <summary>
    /// A collection of message middlewares to be used in the messaging pipeline.
    /// Resolved in specific order based on the configuration.
    /// </summary>
    readonly IEnumerable<IMessageMiddleware> middlewares = config.Middlewares.Select(t => (IMessageMiddleware)provider.GetRequiredService(t));

    /// <summary>
    /// Publishes a message to all registered middlewares. 
    /// This method processes the message asynchronously through each middleware in the order they were registered.
    /// </summary>    
    public async Task Publish<T>(Message<T> message)
    {
        foreach (var m in middlewares)
        {
            await m.ProcessAsync(message);
        }
    }

    public async Task Publish<T>(T payload)
    {
        var message = new Message<T> { Payload = payload };
        await Publish(message);
    }
}
