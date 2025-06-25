namespace Sencilla.Messaging.InMemoryQueue;

/// <summary>
/// Consumer service for specific provider of the messaging system.
/// </summary>
/// <param name="logger"></param>
/// <param name="config"></param>
public class ProviderConsumerService(
    ILogger<ProviderConsumerService> logger,
    IMessageBroker broker,
    ProviderConfig config)
{
    /// <summary>
    /// 
    /// </summary>
    /// <param name="stoppingToken"></param>
    /// <returns></returns>
    public async Task Execute(CancellationToken stoppingToken)
    {
        logger.LogInformation("ProviderConsumerService::ExecuteAsync is called at utctime: {time}", DateTimeOffset.UtcNow);

        // Get the broker and list of queues for broker
        broker.Queues.Names?.ForEach(async queue =>
        {
            //var queueService = new QueueConsumerService(logger, broker.Queues.GetOrCreateStream(queue), config);
        });
    }
}

