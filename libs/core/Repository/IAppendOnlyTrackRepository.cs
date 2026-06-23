namespace Sencilla.Core;

/// <summary>
/// Append-only valid-time repository operations beyond create/supersede — sits alongside
/// <see cref="IReadRepository{TEntity,TKey}"/> / <see cref="IUpdateRepository{TEntity,TKey}"/> so the generic
/// CRUD API can expose them. Like its siblings the constraint is just <see cref="IEntity{TKey}"/> (no
/// append-only marker), so the CRUD controller can resolve it for any entity; only entities that actually
/// implement <see cref="IEntityAppendOnlyTrack"/> have an implementation registered. The EF implementation is
/// <c>Sencilla.Repository.EntityFramework.AppendOnlyTrackRepository</c>.
/// </summary>
public interface IAppendOnlyTrackRepository<TEntity, TKey>
    where TEntity : IEntity<TKey>
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
