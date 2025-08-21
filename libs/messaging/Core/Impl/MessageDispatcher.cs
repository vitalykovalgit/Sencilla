namespace Sencilla.Messaging;

/// <summary>
/// A collection of message middlewares to be used in the messaging pipeline.
/// Resolved in specific order based on the configuration.
/// </summary>
/// <typeparam name="IMessageMiddleware"></typeparam>
[SingletonLifetime]
public class MessageDispatcher(IServiceProvider serviceProvider, MessagingConfig config): IMessageDispatcher
{
    private readonly IEnumerable<IMessageMiddleware> Middlewares = config.Middlewares.Select(m => (IMessageMiddleware)serviceProvider.GetRequiredService(m));

    /// <summary>
    /// Publishes a message to all registered middlewares. 
    /// This method processes the message asynchronously through each middleware in the order they were registered.
    /// </summary>
    public async Task Send<T>(T payload, CancellationToken cancellationToken = default)
    {
        var message = new Message<T> { Payload = payload };
        await Send(message, cancellationToken);
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="message"></param>
    /// <typeparam name="T"></typeparam>
    /// <returns></returns>    
    public async Task Send<T>(Message<T> message, CancellationToken cancellationToken = default)
    {
        if (Middlewares == null) return;

        foreach (var m in Middlewares)
        {
            await m.ProcessAsync(message, cancellationToken);
        }
    }
}
