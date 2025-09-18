namespace Sencilla.Messaging.SignalR;

[DisableInjection]
public class SignalRQueueMiddleware(SignalRProviderConfig config, SignalRStreamProvider streamProvider) : MessageMiddleware, IMessageMiddleware
{
    public async Task ProcessAsync<T>(Message<T>? msg, CancellationToken cancellationToken = default)
    {
        // Validate the message
        if (msg is not null)
            await SendToStream(msg, config, streamProvider);
    }
}