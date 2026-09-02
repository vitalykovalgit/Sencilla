namespace Sencilla.Messaging.SignalR;

public class SignalRStreamProvider(IHubContext<MessagingHub> hubContext, ILoggerFactory loggerFactory) : IMessageStreamProvider
{
    private readonly ConcurrentDictionary<string, IMessageStream> Streams = [];

    public IMessageStream GetOrCreateStream(StreamConfig config)
    {
        if (config?.Name == null)
            throw new ArgumentNullException(nameof(config.Name), "Stream name cannot be null.");

        return Streams.GetOrAdd(config.Name, _ => new SignalRQueue(config.Name, hubContext, loggerFactory.CreateLogger<SignalRQueue>()));
    }

    public IMessageStream? GetStream(StreamConfig config)
    {
        return GetOrCreateStream(config);
    }
}
