namespace Sencilla.Messaging.RabbitMQ;

/// <summary>
/// RabbitMQ hosted service for message consumption
/// </summary>
public class RabbitMQHostedService(
    IRabbitMQConsumer consumer,
    IOptions<RabbitMQOptions> options,
    ILogger<RabbitMQHostedService> logger) : BackgroundService
{
    readonly RabbitMQOptions _options = options.Value;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("RabbitMQ hosted service started");

        try
        {
            // Start consuming from configured queues
            // This is a basic implementation - in practice, you might want to configure
            // multiple queues or discover them dynamically
            var queueName = $"{_options.QueuePrefix}.default";
            await consumer.StartConsumingAsync(queueName, stoppingToken);
        }
        catch (OperationCanceledException)
        {
            logger.LogInformation("RabbitMQ hosted service stopped");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error in RabbitMQ hosted service");
            throw;
        }
    }

    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        logger.LogInformation("Stopping RabbitMQ hosted service");
        await consumer.StopConsumingAsync();
        await base.StopAsync(cancellationToken);
    }
}
