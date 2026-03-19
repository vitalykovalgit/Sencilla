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
