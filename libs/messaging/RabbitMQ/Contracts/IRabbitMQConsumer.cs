namespace Sencilla.Messaging.RabbitMQ;

/// <summary>
/// RabbitMQ message consumer interface
/// </summary>
public interface IRabbitMQConsumer
{
    /// <summary>
    /// Start consuming messages from a queue
    /// </summary>
    /// <param name="queueName">Queue name to consume from</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Task representing the consume operation</returns>
    Task StartConsumingAsync(string queueName, CancellationToken cancellationToken = default);

    /// <summary>
    /// Stop consuming messages
    /// </summary>
    /// <returns>Task representing the stop operation</returns>
    Task StopConsumingAsync();
}
