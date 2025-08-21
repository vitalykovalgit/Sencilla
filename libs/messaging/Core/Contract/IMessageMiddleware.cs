
namespace Sencilla.Messaging;

/// <summary>
/// Defines a middleware interface for processing messages in a messaging system.
/// </summary>
public interface IMessageMiddleware
{
    /// <summary>
    /// Processes a message asynchronously.
    /// </summary>
    Task ProcessAsync<T>(Message<T>? msg, CancellationToken cancellationToken = default);
}