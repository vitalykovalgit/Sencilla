
namespace Sencilla.Messaging.InMemoryQueue;

[DisableInjection]
public class InMemoryQueueMiddleware(InMemoryProviderConfig config, InMemoryStreamProvider streamProvider): MessageMiddleware, IMessageMiddleware
{
    public async Task ProcessAsync<T>(Message<T>? msg, CancellationToken cancellationToken = default)
    {
        // Validate the message
        if (msg is not null)
            await SendToStream(msg, config, streamProvider);        
    }
}
