namespace Sencilla.Messaging.SignalR;

public class SignalRQueueMiddleware(SignalRProviderConfig config, SignalRStreamProvider streamProvider) : MessageMiddleware, IMessageMiddleware
{
    public async Task HandleAsync<T>(Message<T> message, Func<Message<T>, CancellationToken, Task> next, CancellationToken cancellationToken = default)
    {
        await SendToStream(message, config, streamProvider);
        await next(message, cancellationToken);
    }
}