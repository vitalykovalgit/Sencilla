namespace Sencilla.Messaging;

/// <summary>
/// 
/// </summary>
[DisableInjection]
public class ConsumerConfig
{
    public ConsumerConfig(ConsumerConfig? config = null)
    {
        if (config != null)
        {
            AutoAck = config.AutoAck;
            Exclusive = config.Exclusive;

            StreamName = config.StreamName;
            StreamSubscription = config.StreamSubscription;

            PrefetchCount = config.PrefetchCount;
            PoolingIntervalInMs = config.PoolingIntervalInMs;
            MaxConcurrentHandlers = config.MaxConcurrentHandlers;
        }
    }

    /// <summary>
    /// Name of the stream 
    /// </summary>
    /// <value></value>
    public string? StreamName { get; set; }

    /// <summary>
    /// 
    /// </summary>
    /// <value></value>
    public string? StreamSubscription { get; set; }

    /// <summary>
    /// The maximum number of concurrent handlers for this queue.
    /// This is used to limit the number of message handlers that can process messages concurrently.
    /// </summary>
    public int MaxConcurrentHandlers { get; set; } = 1;

    /// <summary>
    /// Indicates whether the consumer should use asynchronous message handlers.
    /// </summary>
    public bool AsyncHandlers { get; set; } = false;

    /// <summary>
    /// The number of messages to prefetch for this queue.  
    /// This is used to optimize message consumption by preloading messages into the consumer's buffer.
    /// </summary>
    public int PrefetchCount { get; set; } = 1;

    /// <summary>
    /// Indicates whether the consumer should automatically acknowledge messages after processing them.
    /// </summary>
    public bool AutoAck { get; set; } = true;

    /// <summary>
    /// The interval at which the consumer should poll for new messages.
    /// </summary>
    /// <value></value>
    public int PoolingIntervalInMs { get; set; } = 1000;

    /// <summary>
    /// Indicates whether the consumer should be exclusive to this queue.   
    /// An exclusive consumer will be the only consumer for the queue, and it will be removed if it disconnects.
    /// </summary>
    public bool Exclusive { get; set; } = false;
}
