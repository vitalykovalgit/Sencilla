namespace Sencilla.Messaging;

/// <summary>
/// Represents a queue or topic 
/// </summary>
public interface IMessageStreamReader
{
    Task<string?> Read(CancellationToken cancellationToken = default);
    Task<Message<T>?> Read<T>(CancellationToken cancellationToken = default);
}

/// <summary>
/// Represents a writer for a message stream.
/// To queue or topic
/// </summary>
public interface IMessageStreamWriter
{
    Task Write<T>(Message<T>? message, CancellationToken cancellationToken = default);
}

public interface IMessageStream : IMessageStreamReader, IMessageStreamWriter
{
    string Name { get; }
    /// <summary>
    /// Gets the name of the message stream.
    /// </summary>
}

/// <summary>
/// Opt-in acknowledgement for streams whose messages outlive the read — a durable queue must learn
/// the outcome or its rows never reach a terminal state. <see cref="MessageStreamConsumer"/> calls
/// these only when the stream implements this interface; a fire-and-forget stream simply doesn't.
/// The message id comes from the envelope the stream itself wrote, so no read handle is needed.
/// </summary>
public interface IMessageStreamAck
{
    /// <summary>All handlers completed. Mark the message terminally succeeded.</summary>
    Task Ack(Guid messageId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Acknowledge from inside the handler's scope, so a durable store can make the acknowledgement
    /// part of the handler's own transaction: the domain change and "this message is done" then
    /// commit together, which makes a redelivery impossible rather than merely tolerated. A stream
    /// with no such store falls back to the plain ack.
    /// </summary>
    Task Ack(Guid messageId, IServiceProvider scopedProvider, CancellationToken cancellationToken = default)
        => Ack(messageId, cancellationToken);

    /// <summary>
    /// Processing failed. <paramref name="retryable"/> false means the message can never succeed
    /// (unresolvable payload type, no registered handler) and must go terminal immediately rather
    /// than burn attempts; true means re-queue subject to the stream's attempt/backoff policy.
    /// Returns true when this attempt was the last one — the message is now terminally failed,
    /// which is what lets the consumer raise <see cref="MessageFailed{T}"/> exactly once.
    /// </summary>
    Task<bool> Nack(Guid messageId, string error, bool retryable, CancellationToken cancellationToken = default);
}
