namespace Sencilla.Messaging;

public class QueueConfig
{
    /// <summary>
    /// The name of the queue to be consumed.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// The maximum number of concurrent consumers for this queue.
    /// </summary>
    public int MaxConcurrentConsumers { get; set; } = 1;

    /// <summary>
    /// The maximum number of concurrent handlers for this queue.
    /// This is used to limit the number of message handlers that can process messages concurrently.
    /// </summary>
    public int MaxConcurrentHandlers { get; set; } = 1;

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
    /// Indicates whether the queue is durable.
    /// A durable queue will survive broker restarts and retain messages.
    /// </summary>
    public bool Durable { get; set; } = true;

    /// <summary>
    /// Indicates whether the consumer should be exclusive to this queue.   
    /// An exclusive consumer will be the only consumer for the queue, and it will be removed if it disconnects.
    /// </summary>
    public bool Exclusive { get; set; } = false;


    /// <summary>
    /// 
    /// </summary>
    public IEnumerable<ProviderConfig> Providers { get; set; } = [];
}

