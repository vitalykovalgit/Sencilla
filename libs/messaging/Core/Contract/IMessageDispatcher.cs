namespace Sencilla.Messaging;

/// <summary>
/// Represents a mechanism for publishing events asynchronously.
/// </summary>
public interface IMessageDispatcher
{
    /// <summary>
    /// Publishes a message asynchronously to the message dispatching mechanism.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="message"></param>
    /// <returns></returns>
    Task Publish<T>(T message);

    /// <summary>
    /// Publishes a message asynchronously to the message dispatching mechanism.
    /// </summary>
    /// <typeparam name="T">The type of the message to be published. Must be a reference type.</typeparam>
    /// <param name="message">The message object to be published.</param>
    /// <returns> A task representing the asynchronous operation.</returns>
    Task Publish<T>(Message<T> message);
    
}

