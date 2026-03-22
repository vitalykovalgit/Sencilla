namespace Sencilla.Messaging.RabbitMQ;

public class RabbitMQHealthCheck(
    IRabbitMQConnectionFactory connectionFactory,
    ILogger<RabbitMQHealthCheck> logger) : IHealthCheck
{
    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        try
        {
            await using var channel = await connectionFactory.CreateChannelAsync();

            var testQueueName = $"health-check-{Guid.NewGuid()}";
            await channel.QueueDeclareAsync(testQueueName, false, true, true);
            await channel.QueueDeleteAsync(testQueueName);

            logger.LogDebug("RabbitMQ health check passed");
            return HealthCheckResult.Healthy("RabbitMQ is healthy");
        }
        catch (OperationCanceledException)
        {
            logger.LogWarning("RabbitMQ health check was cancelled");
            return HealthCheckResult.Unhealthy("RabbitMQ health check was cancelled");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "RabbitMQ health check failed");
            return HealthCheckResult.Unhealthy("RabbitMQ is unhealthy", ex);
        }
    }
}
