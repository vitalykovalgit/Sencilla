namespace Sencilla.Messaging;

/// <summary>
/// Resolves and executes message handlers from a scoped service provider.
/// </summary>
public interface IMessageHandlerExecutor
{
    /// <summary>
    /// Resolves handlers for the message type and executes them within the provided scope.
    /// </summary>
    Task ExecuteAsync<T>(Message<T> message, IServiceProvider scopedProvider, CancellationToken cancellationToken = default);
}
