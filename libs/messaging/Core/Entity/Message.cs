namespace Sencilla.Messaging;

/// <summary>
/// 
/// </summary>
/// <typeparam name="T"></typeparam>
public class Message<T> where T : class
{
    /// <summary>
    /// Gets the unique identifier associated with the message.
    /// </summary>
    /// <remarks>
    /// This identifier serves as a unique reference for the message, enabling
    /// tracking and distinguishing it from other messages.
    /// </remarks>
    public Guid Id { get; init; } = Guid.NewGuid();

    /// <summary>
    /// Gets the correlation identifier associated with the message.
    /// </summary>
    /// <remarks>
    /// The correlation identifier is used to group related messages together,
    /// providing a way to track and associate messages that are part of the same
    /// conversation or transaction.
    /// </remarks>
    public Guid CorrelationId { get; init; }

    /// <summary>
    /// 
    /// </summary>
    public MessageType Type { get; init; }

    /// <summary>
    /// 
    /// </summary>
    public MessageState State { get; set; } = MessageState.New;

    /// <summary>
    /// 
    /// </summary>
    public string? Name { get; init; }

    /// <summary>
    /// 
    /// </summary>
    public DateTime CreatedAt { get; init; } = DateTime.UtcNow;

    /// <summary>
    /// 
    /// </summary>
    public DateTime? ProcessedAt { get; set; }


    /// <summary>
    /// Contains dot net type name including name pace
    /// </summary>
    public string? Namespace { get; init; }

    /// <summary>
    /// 
    /// </summary>
    public string? Error { get; set; }

    /// <summary>
    /// 
    /// </summary>
    public T? Payload { get; init; }
    
    /// <summary>
    /// Implicitly converts from type T to Message&lt;T&gt;.
    /// </summary>
    /// <param name="payload">The payload to convert.</param>
    /// <returns>A new Message&lt;T&gt; instance with the specified payload.</returns>
    public static implicit operator Message<T>(T payload)
    {
        return new Message<T> { Payload = payload };
    }
    
}