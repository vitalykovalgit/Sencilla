namespace Sencilla.Messaging;

/// <summary>
/// Dispatches messages through a pipeline of registered middlewares.
/// Middlewares are resolved once at construction time and executed in registration order.
/// </summary>
[SingletonLifetime]
public class MessageDispatcher(
    IServiceProvider serviceProvider,
    MessagingConfig config,
    ILogger<MessageDispatcher>? logger = null) : IMessageDispatcher
{
    private readonly IMessageMiddleware[] Middlewares = config.Middlewares
        .Select(m => (IMessageMiddleware)serviceProvider.GetRequiredService(m))
        .ToArray();

    /// <summary>
    /// Wraps the payload in a <see cref="Message{T}"/> and dispatches it through all registered middlewares.
    /// </summary>
    /// <param name="payload">The payload to send.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    public Task Send<T>(T payload, CancellationToken cancellationToken = default)
    {
        var message = new Message<T> { Payload = payload };
        return Send(message, cancellationToken);
    }

    /// <summary>
    /// Dispatches a message through all registered middlewares in the order they were registered.
    /// </summary>
    /// <param name="message">The message to dispatch.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    public async Task Send<T>(Message<T> message, CancellationToken cancellationToken = default)
    {
        for (var i = 0; i < Middlewares.Length; i++)
        {
            cancellationToken.ThrowIfCancellationRequested();

            try
            {
                await Middlewares[i].ProcessAsync(message, cancellationToken);
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                logger?.LogError(ex, "Middleware {Middleware} failed processing message {MessageId}", Middlewares[i].GetType().Name, message.Id);
                throw;
            }
        }
    }
}
