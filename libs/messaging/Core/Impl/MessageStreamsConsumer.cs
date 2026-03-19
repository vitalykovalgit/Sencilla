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
    /// <summary>
    /// Called automatically by the host when AutoStartConsumers is true (default).
    /// </summary>
    protected override Task ExecuteAsync(CancellationToken stoppingToken)
    {
        return ExecuteConsumersAsync(stoppingToken);
    }

    /// <summary>
    /// Starts all configured consumers and awaits their completion.
    /// Call this directly when AutoStartConsumers is false and you manage the lifecycle from your own BackgroundService.
    /// </summary>
    public Task ExecuteConsumersAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("StreamConsumerManager running at: {Time}", DateTimeOffset.UtcNow);

        var tasks = new List<Task>();

        foreach (var consumerConfig in config.Consumers.GetConsumers())
        {
            if (consumerConfig.StreamName is null)
            {
                logger.LogWarning("Consumer configuration is missing a stream name. Skipping consumer creation.");
                continue;
            }

            var streamConfig = config.Streams.GetConfig(consumerConfig.StreamName);
            if (streamConfig is null)
            {
                logger.LogWarning("Stream configuration for '{StreamName}' not found. Skipping consumer creation.", consumerConfig.StreamName);
                continue;
            }

            var consumerLogger = loggerFactory.CreateLogger<MessageStreamConsumer>();
            var consumer = new MessageStreamConsumer(consumerLogger, scopeFactory, executor, provider, consumerConfig, streamConfig);
            tasks.Add(consumer.Execute(stoppingToken));
        }

        if (tasks.Count == 0)
        {
            logger.LogWarning("No consumers were created. Check stream and consumer configuration.");
            return Task.CompletedTask;
        }

        return Task.WhenAll(tasks);
    }
}

