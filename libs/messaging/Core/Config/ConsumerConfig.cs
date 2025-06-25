namespace Sencilla.Messaging;

/// <summary>
/// 
/// </summary>
public class ConsumersConfig: QueueConfig
{
    /// <summary>
    /// A list of queues to be consumed.
    /// </summary>
    public Dictionary<string, QueueConfig> Queues { get; set; } = [];

    /// <summary>
    /// Adds a queue to the list of queues to be consumed.
    /// </summary>
    /// <param name="queue">The queue configuration to add.</param>
    public void ForQueue(string name, Action<QueueConfig> config)
    {
        var queue = Queues.ContainsKey(name) ? Queues[name] : new QueueConfig { Name = name };
        config(queue);

        Queues[name] = queue;
    }
}

