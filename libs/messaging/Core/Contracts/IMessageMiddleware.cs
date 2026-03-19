namespace Sencilla.Messaging;

/// <summary>
/// Defines a middleware interface for processing messages in a chain-of-responsibility pipeline.
/// Each middleware can perform work before and/or after calling next to continue the chain.
/// </summary>
public interface IMessageMiddleware
{
    /// <summary>
    /// Handles a message and optionally delegates to the next middleware in the chain.
    /// </summary>
    Task HandleAsync<T>(Message<T> message, Func<Message<T>, CancellationToken, Task> next, CancellationToken cancellationToken = default);
}
