namespace Sencilla.Messaging.RabbitMQ;

/// <summary>
/// RabbitMQ topology manager interface for declaring exchanges, queues, and bindings
/// </summary>
public interface IRabbitMQTopologyManager
{
    /// <summary>
    /// Declare an exchange
    /// </summary>
    /// <param name="exchangeName">Exchange name</param>
    /// <param name="exchangeType">Exchange type (direct, topic, fanout, headers)</param>
    /// <param name="durable">Whether the exchange is durable</param>
    /// <param name="autoDelete">Whether the exchange auto-deletes</param>
    /// <returns>Task representing the declare operation</returns>
    Task DeclareExchangeAsync(string exchangeName, string exchangeType = ExchangeType.Direct, bool durable = true, bool autoDelete = false);

    /// <summary>
    /// Declare a queue
    /// </summary>
    /// <param name="queueName">Queue name</param>
    /// <param name="durable">Whether the queue is durable</param>
    /// <param name="exclusive">Whether the queue is exclusive</param>
    /// <param name="autoDelete">Whether the queue auto-deletes</param>
    /// <param name="arguments">Additional queue arguments</param>
    /// <returns>Task representing the declare operation</returns>
    Task DeclareQueueAsync(string queueName, bool durable = true, bool exclusive = false, bool autoDelete = false, IDictionary<string, object>? arguments = null);

    /// <summary>
    /// Bind a queue to an exchange
    /// </summary>
    /// <param name="queueName">Queue name</param>
    /// <param name="exchangeName">Exchange name</param>
    /// <param name="routingKey">Routing key</param>
    /// <returns>Task representing the bind operation</returns>
    Task BindQueueAsync(string queueName, string exchangeName, string routingKey);

    /// <summary>
    /// Setup dead letter queue topology
    /// </summary>
    /// <param name="mainQueueName">Main queue name</param>
    /// <returns>Task representing the setup operation</returns>
    Task SetupDeadLetterQueueAsync(string mainQueueName);
}
