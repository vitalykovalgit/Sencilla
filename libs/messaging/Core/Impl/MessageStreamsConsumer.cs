namespace Sencilla.Messaging;

public class MessageStreamsConsumer<TProvider, TConfig>(
    ILogger<MessageStreamsConsumer<TProvider, TConfig>> logger, TProvider provider, TConfig config) : BackgroundService
    where TProvider : IMessageStreamProvider
    where TConfig : ProviderConfig
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("StreamConsumerManager running at: {time}", DateTimeOffset.Now);

        List<Task> tasks = [];

        config.Consumers.GetConsumers().ForEach(consumerConfig =>
        {
            if (consumerConfig.StreamName is null)
            {
                logger.LogWarning("Consumer configuration for '{ConsumerName}' is missing a stream name. Skipping consumer creation.", consumerConfig.StreamName);
                return;
            }

            var streamConfig = config.Streams.GetConfig(consumerConfig.StreamName);
            if (streamConfig is null)
            {
                logger.LogWarning("Stream configuration for '{StreamName}' not found. Skipping consumer creation.", consumerConfig.StreamName);
                return;
            }

            var consumer = new MessageStreamConsumer(null, provider, consumerConfig, streamConfig);
            tasks.Add(consumer.Execute(stoppingToken));
        });

        await Task.WhenAll(tasks);
    }
}


