namespace Sencilla.Messaging.RabbitMQ;

public class RabbitMQStreamProvider(
    IRabbitMQConnectionFactory connectionFactory,
    RabbitMQProviderConfig config) : IMessageStreamProvider
{
    private readonly ConcurrentDictionary<string, IMessageStream> Streams = [];

    public IMessageStream GetOrCreateStream(StreamConfig streamConfig)
    {
        if (streamConfig?.Name is null)
            throw new ArgumentNullException(nameof(streamConfig.Name), "Stream name cannot be null.");

        return Streams.GetOrAdd(streamConfig.Name, _ =>
            new RabbitMQStream(connectionFactory, streamConfig, config.Options));
    }

    public IMessageStream? GetStream(StreamConfig streamConfig)
    {
        if (streamConfig?.Name is null)
            return null;

        Streams.TryGetValue(streamConfig.Name, out var stream);
        return stream;
    }
}
