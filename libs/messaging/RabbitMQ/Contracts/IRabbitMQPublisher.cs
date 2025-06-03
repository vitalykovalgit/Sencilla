namespace Sencilla.Messaging.RabbitMQ;

/// <summary>
/// RabbitMQ message publisher interface
/// </summary>
public interface IRabbitMQPublisher
{
    /// <summary>
    /// Publish a message to RabbitMQ
    /// </summary>
    /// <typeparam name="T">Message type</typeparam>
    /// <param name="message">Message to publish</param>
    /// <param name="exchange">Exchange name (optional)</param>
    /// <param name="routingKey">Routing key (optional)</param>
    /// <returns>Task representing the publish operation</returns>
    Task PublishAsync<T>(T message, string? exchange = null, string? routingKey = null) where T : class;

    /// <summary>
    /// Publish a message to a specific queue
    /// </summary>
    /// <typeparam name="T">Message type</typeparam>
    /// <param name="message">Message to publish</param>
    /// <param name="queueName">Queue name</param>
    /// <returns>Task representing the publish operation</returns>
    Task PublishToQueueAsync<T>(T message, string queueName) where T : class;
}
