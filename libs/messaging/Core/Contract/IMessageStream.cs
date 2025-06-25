namespace Sencilla.Messaging;

/// <summary>
/// Represents a queue or topic 
/// </summary>
public interface IMessageStreamReader
{
    string Name { get; }
    Task<T> Dequeue<T>(CancellationToken cancellationToken = default);
}

/// <summary>
/// 
/// </summary>
public interface IMessageStreamWriter
{
    string Name { get; }
    Task Enqueue<T>(T? peyload, CancellationToken cancellationToken = default);
    Task Enqueue<T>(Message<T>? message, CancellationToken cancellationToken = default);
}

public interface IMessageStream : IMessageStreamReader, IMessageStreamWriter
{
    /// <summary>
    /// Gets the name of the message stream.
    /// </summary>
}

