namespace Sencilla.Messaging;

/// <summary>
/// Dispatches messages through a chain-of-responsibility pipeline of registered middlewares.
/// The pipeline is built once per message type T and cached for subsequent calls.
/// </summary>
public class MessageDispatcher(
    IServiceProvider serviceProvider,
    MessagingConfig config,
    ILogger<MessageDispatcher>? logger = null) : IMessageDispatcher
{
    private readonly IMessageMiddleware[] Middlewares = config.Middlewares
        .Select(m => (IMessageMiddleware)serviceProvider.GetRequiredService(m))
        .ToArray();

    private readonly ConcurrentDictionary<Type, object> PipelineCache = [];

    /// <summary>
    /// Wraps the payload in a <see cref="Message{T}"/> and dispatches it through the middleware chain.
    /// </summary>
    public Task Send<T>(T payload, CancellationToken cancellationToken = default)
    {
        var message = new Message<T> { Payload = payload };
        return Send(message, cancellationToken);
    }

    /// <summary>
    /// Dispatches a message through the middleware chain built from registered middlewares.
    /// </summary>
    public Task Send<T>(Message<T> message, CancellationToken cancellationToken = default)
    {
        var pipeline = (Func<Message<T>, CancellationToken, Task>)PipelineCache.GetOrAdd(
            typeof(T),
            _ => BuildPipeline<T>());

        return pipeline(message, cancellationToken);
    }

    private Func<Message<T>, CancellationToken, Task> BuildPipeline<T>()
    {
        Func<Message<T>, CancellationToken, Task> next = (_, _) => Task.CompletedTask;

        for (var i = Middlewares.Length - 1; i >= 0; i--)
        {
            var middleware = Middlewares[i];
            var current = next;
            next = (msg, token) =>
            {
                token.ThrowIfCancellationRequested();

                try
                {
                    return middleware.HandleAsync(msg, current, token);
                }
                catch (OperationCanceledException)
                {
                    throw;
                }
                catch (Exception ex)
                {
                    logger?.LogError(ex, "Middleware {Middleware} failed processing message {MessageId}",
                        middleware.GetType().Name, msg.Id);
                    throw;
                }
            };
        }

        return next;
    }
}
