namespace Sencilla.Core;

/// <summary>
/// Represents a database transaction abstraction that is implementation-agnostic
/// </summary>
public interface IDbTransaction : IDisposable, IAsyncDisposable
{
    /// <summary>
    /// Commits all changes made to the database in the current transaction
    /// </summary>
    /// <param name="token">Cancellation token</param>
    Task CommitAsync(CancellationToken token = default);

    /// <summary>
    /// Discards all changes made to the database in the current transaction
    /// </summary>
    /// <param name="token">Cancellation token</param>
    Task RollbackAsync(CancellationToken token = default);
}
