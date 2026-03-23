namespace Sencilla.Messaging;

/// <summary>
/// Attribute to specify stream routing for messages.
/// Supports optional retry and instance configuration.
/// </summary>
/// <example>
/// <code>
/// [Stream("my-queue", MaxRetries = 3, Instance = 5)]
/// public class MyMessage { }
/// </code>
/// </example>
/// <param name="names">One or more stream names.</param>
[AttributeUsage(AttributeTargets.Class)]
public class StreamAttribute(params string[] names) : Attribute
{
    /// <summary>
    /// Stream names this message is routed to.
    /// </summary>
    public IEnumerable<string> Names { get; } = names;

    /// <summary>
    /// Maximum number of retry attempts for failed message processing.
    /// </summary>
    public int MaxRetries { get; set; }

    /// <summary>
    /// Number of consumer instances per queue.
    /// </summary>
    public int Instance { get; set; } = 1;
}
