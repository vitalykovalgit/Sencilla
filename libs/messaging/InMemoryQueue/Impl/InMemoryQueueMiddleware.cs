namespace Sencilla.Messaging.InMemoryQueue;

public class InMemoryQueueMiddleware(InMemoryProviderConfig config, InMemoryStreamProvider streamProvider) : MessageMiddleware, IMessageMiddleware
{
    public async Task HandleAsync<T>(Message<T> message, Func<Message<T>, CancellationToken, Task> next, CancellationToken cancellationToken = default)
    {
        await SendToStream(message, config, streamProvider);
        await next(message, cancellationToken);
    }
}
