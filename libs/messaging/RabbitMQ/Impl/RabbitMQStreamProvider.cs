namespace Sencilla.Messaging.RabbitMQ;

public class RabbitMQStreamProvider(
    IRabbitMQConnectionFactory connectionFactory,
    RabbitMQProviderConfig config,
    ILoggerFactory loggerFactory) : IMessageStreamProvider
{
    private readonly ConcurrentDictionary<string, IMessageStream> Streams = [];

    public IMessageStream GetOrCreateStream(StreamConfig streamConfig)
    {
        if (streamConfig?.Name is null)
            throw new ArgumentNullException(nameof(streamConfig.Name), "Stream name cannot be null.");

        return Streams.GetOrAdd(streamConfig.Name, name =>
            new RabbitMQStream(
                connectionFactory,
                streamConfig,
                config.Options,
                // Null on a producer-only stream, which never consumes and so never acknowledges.
                config.Consumers.GetConsumers().FirstOrDefault(c => c.StreamName == name),
                loggerFactory.CreateLogger<RabbitMQStream>()));
    }

    public IMessageStream? GetStream(StreamConfig streamConfig)
    {
        if (streamConfig?.Name is null)
            return null;

        Streams.TryGetValue(streamConfig.Name, out var stream);
        return stream;
    }
}
