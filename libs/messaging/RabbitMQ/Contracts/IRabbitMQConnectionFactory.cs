namespace Sencilla.Messaging.RabbitMQ;

/// <summary>
/// RabbitMQ connection factory interface
/// </summary>
public interface IRabbitMQConnectionFactory
{
    /// <summary>
    /// Create a new RabbitMQ connection
    /// </summary>
    /// <returns>RabbitMQ connection</returns>
    Task<IConnection> CreateConnectionAsync();

    /// <summary>
    /// Create a new RabbitMQ channel
    /// </summary>
    /// <returns>RabbitMQ channel</returns>
    Task<IChannel> CreateChannelAsync();
}
