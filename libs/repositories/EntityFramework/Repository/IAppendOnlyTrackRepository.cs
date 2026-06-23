namespace Sencilla.Repository.EntityFramework;

/// <summary>
/// Append-only valid-time repository operations beyond create/supersede. Implemented by
/// <see cref="AppendOnlyTrackRepository{TEntity,TContext,TKey}"/>; resolve it to cancel a scheduled
/// (not-yet-active) version of an <see cref="IEntityAppendOnlyTrack"/> entity.
/// </summary>
public interface IAppendOnlyTrackRepository<TEntity, TKey>
    where TEntity : class, IEntity<TKey>, IEntityAppendOnlyTrack
{
    /// <summary>
    /// Cancels the latest scheduled version — the open future tail (<c>ActiveTo == null</c> and
    /// <c>ActiveFrom &gt; now</c>) — for the business key carried by <paramref name="key"/>, reopening the
    /// version it had superseded. LIFO: only the most-recently scheduled change is cancellable; to drop an
    /// earlier one, cancel the later ones first. Throws <see cref="Sencilla.Core.BadRequestException"/> when
    /// the open version is already active (nothing is scheduled).
    /// </summary>
    Task CancelPending(TEntity key, CancellationToken token = default);
}
