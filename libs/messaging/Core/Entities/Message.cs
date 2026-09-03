namespace Sencilla.Messaging;

public class Message
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
    /// Short type name of the payload, stamped by the dispatcher — diagnostics only.
    /// </summary>
    public string? Name { get; set; }

    /// <summary>
    /// 
    /// </summary>
    public DateTime CreatedAt { get; init; } = DateTime.UtcNow;

    /// <summary>
    /// 
    /// </summary>
    public DateTime? ProcessedAt { get; set; }


    /// <summary>
    /// The payload's .NET type FullName, stamped by the dispatcher. Diagnostics only —
    /// resolution goes through <see cref="PayloadType"/>, which survives renames.
    /// </summary>
    public string? Namespace { get; set; }

    /// <summary>
    /// Resolution key for the payload type: the [PayloadType] alias when the type declares one,
    /// otherwise its FullName. Stamped by the dispatcher, resolved by
    /// <see cref="PayloadTypeRegistry"/> on the consuming side. Persisted transports store this
    /// forever, so prefer an explicit alias over a FullName you may want to rename later.
    /// </summary>
    public string? PayloadType { get; set; }

    /// <summary>
    /// Delivery attempts made for this message, stamped by durable transports on read (0 for
    /// fire-and-forget ones). Handlers can use it to tell a first run from a retry.
    /// </summary>
    public int Attempts { get; set; }

    /// <summary>
    /// 
    /// </summary>
    public string? Error { get; set; }
    
    /// <summary>
    /// The subject entity this message is about, when there is one — set explicitly by the sender.
    /// Durable transports store it as an indexed column so callers can ask "is anything queued for
    /// this entity?" without scanning payload JSON.
    /// </summary>
    public Guid? EntityId { get; set; }

    public Guid? UserId { get; set; }
    public string? UserEmail { get; set; }

    /// <summary>
    /// Set or retrieve metadata for the message.
    /// </summary>
    /// <value></value>
    public Dictionary<string, string>? Metadata { get; set; }
}

/// <summary>
/// 
/// </summary>
/// <typeparam name="T"></typeparam>
public class Message<T>: Message
{
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