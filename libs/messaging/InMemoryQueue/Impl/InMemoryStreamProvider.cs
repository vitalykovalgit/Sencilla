namespace Sencilla.Messaging.InMemoryQueue;

public class InMemoryStreamProvider : IMessageStreamProvider
{
    private readonly ConcurrentDictionary<string, IMessageStream> Streams = [];

    public IMessageStream GetOrCreateStream(StreamConfig config)
    {
        if (config?.Name is null)
            throw new ArgumentNullException(nameof(config.Name), "Stream name cannot be null.");

        return Streams.GetOrAdd(config.Name, _ => new InMemoryQueue(config.Name, config.MaxQueueSize));
    }

    public IMessageStream? GetStream(StreamConfig config)
    {
        if (config?.Name is null)
            return null;

        Streams.TryGetValue(config.Name, out var stream);
        return stream;
    }
}