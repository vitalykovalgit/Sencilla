namespace Sencilla.Messaging;

public class MessageStreamsConsumer<TProvider, TConfig>(
    ILogger<MessageStreamsConsumer<TProvider, TConfig>> logger,
    ILoggerFactory loggerFactory,
    IServiceScopeFactory scopeFactory,
    IMessageHandlerExecutor executor,
    TProvider provider,
    TConfig config) : BackgroundService
    where TProvider : IMessageStreamProvider
    where TConfig : ProviderConfig
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("StreamConsumerManager running at: {Time}", DateTimeOffset.Now);

        List<Task> tasks = [];

        config.Consumers.GetConsumers().ForEach(consumerConfig =>
        {
            if (consumerConfig.StreamName is null)
            {
                logger.LogWarning("Consumer configuration is missing a stream name. Skipping consumer creation.");
                return;
            }

            var streamConfig = config.Streams.GetConfig(consumerConfig.StreamName);
            if (streamConfig is null)
            {
                logger.LogWarning("Stream configuration for '{StreamName}' not found. Skipping consumer creation.", consumerConfig.StreamName);
                return;
            }

            var consumerLogger = loggerFactory.CreateLogger<MessageStreamConsumer>();
            var consumer = new MessageStreamConsumer(consumerLogger, scopeFactory, executor, provider, consumerConfig, streamConfig);
            tasks.Add(consumer.Execute(stoppingToken));
        });

        await Task.WhenAll(tasks);
    }
}

