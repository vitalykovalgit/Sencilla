namespace Sencilla.Messaging.RabbitMQ;

public class RabbitMQProviderOptions
{
    /// <summary>
    /// RabbitMQ connection string (AMQP URI).
    /// </summary>
    public string ConnectionString { get; set; } = "amqp://guest:guest@localhost:5672/";

    /// <summary>
    /// Default exchange name. Empty string means the default (nameless) exchange.
    /// </summary>
    public string DefaultExchange { get; set; } = "";

    /// <summary>
    /// Consumer prefetch count — controls how many unacknowledged messages
    /// the broker delivers to a consumer before waiting for ACKs.
    /// </summary>
    public ushort PrefetchCount { get; set; } = 10;

    /// <summary>
    /// Whether to declare exchanges and queues automatically on first use.
    /// </summary>
    public bool AutoDeclareTopology { get; set; } = true;

    /// <summary>
    /// Enable dead letter queue support. When true, queues are created with
    /// x-dead-letter-exchange and x-dead-letter-routing-key arguments.
    /// </summary>
    public bool EnableDeadLetterQueue { get; set; } = true;

    /// <summary>
    /// Dead letter exchange name.
    /// </summary>
    public string DeadLetterExchange { get; set; } = "dlx";

    /// <summary>
    /// Dead letter queue name suffix. The full DLQ name is "{queueName}.{suffix}".
    /// </summary>
    public string DeadLetterQueue { get; set; } = "dlq";

    /// <summary>
    /// Message time-to-live in milliseconds. 0 means no expiration.
    /// </summary>
    public int MessageTtlMilliseconds { get; set; } = 0;
}
