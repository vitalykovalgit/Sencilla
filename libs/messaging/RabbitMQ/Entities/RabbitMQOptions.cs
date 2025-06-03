namespace Sencilla.Messaging.RabbitMQ;

/// <summary>
/// Configuration options for RabbitMQ messaging
/// </summary>
public class RabbitMQOptions
{
    /// <summary>
    /// RabbitMQ connection string
    /// </summary>
    public string ConnectionString { get; set; } = "amqp://guest:guest@localhost:5672/";

    /// <summary>
    /// Default exchange name
    /// </summary>
    public string DefaultExchange { get; set; } = "";

    /// <summary>
    /// Default queue name prefix
    /// </summary>
    public string QueuePrefix { get; set; } = "sencilla";

    /// <summary>
    /// Maximum retry attempts for failed messages
    /// </summary>
    public int MaxRetryAttempts { get; set; } = 3;

    /// <summary>
    /// Retry delay in milliseconds
    /// </summary>
    public int RetryDelayMilliseconds { get; set; } = 5000;

    /// <summary>
    /// Enable dead letter queue
    /// </summary>
    public bool EnableDeadLetterQueue { get; set; } = true;

    /// <summary>
    /// Dead letter exchange name
    /// </summary>
    public string DeadLetterExchange { get; set; } = "dlx";

    /// <summary>
    /// Dead letter queue name
    /// </summary>
    public string DeadLetterQueue { get; set; } = "dlq";

    /// <summary>
    /// Message time-to-live in milliseconds (0 means no expiration)
    /// </summary>
    public int MessageTtlMilliseconds { get; set; } = 0;

    /// <summary>
    /// Whether to declare exchanges and queues automatically
    /// </summary>
    public bool AutoDeclareTopology { get; set; } = true;

    /// <summary>
    /// Consumer prefetch count
    /// </summary>
    public ushort PrefetchCount { get; set; } = 10;
}
