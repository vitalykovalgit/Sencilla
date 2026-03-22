namespace Sencilla.Messaging.RabbitMQ;

public class RabbitMQMiddleware(RabbitMQProviderConfig config, RabbitMQStreamProvider streamProvider) : MessageMiddleware, IMessageMiddleware
{
    public async Task HandleAsync<T>(Message<T> message, Func<Message<T>, CancellationToken, Task> next, CancellationToken cancellationToken = default)
    {
        await SendToStream(message, config, streamProvider);
        await next(message, cancellationToken);
    }
}
