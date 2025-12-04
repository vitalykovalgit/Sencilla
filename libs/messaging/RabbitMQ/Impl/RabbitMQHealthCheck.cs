namespace Sencilla.Messaging.RabbitMQ;

/// <summary>
/// RabbitMQ health check implementation
/// </summary>
public class RabbitMQHealthCheck : IHealthCheck
{
    readonly IRabbitMQConnectionFactory _connectionFactory;
    readonly ILogger<RabbitMQHealthCheck> _logger;

    public RabbitMQHealthCheck(IRabbitMQConnectionFactory connectionFactory, ILogger<RabbitMQHealthCheck> logger)
    {
        _connectionFactory = connectionFactory;
        _logger = logger;
    }

    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        try
        {
            await using var channel = await _connectionFactory.CreateChannelAsync();
            
            // Test queue declare operation
            var testQueueName = $"health-check-{Guid.NewGuid()}";
            await channel.QueueDeclareAsync(testQueueName, false, true, true);
            await channel.QueueDeleteAsync(testQueueName);

            _logger.LogDebug("RabbitMQ health check passed");
            return HealthCheckResult.Healthy("RabbitMQ is healthy");
        }
        catch (OperationCanceledException)
        {
            _logger.LogWarning("RabbitMQ health check was cancelled");
            return HealthCheckResult.Unhealthy("RabbitMQ health check was cancelled");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "RabbitMQ health check failed");
            return HealthCheckResult.Unhealthy("RabbitMQ is unhealthy", ex);
        }
    }
}
