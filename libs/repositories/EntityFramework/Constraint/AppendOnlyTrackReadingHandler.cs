namespace Sencilla.Repository.EntityFramework;

/// <summary>
/// Applies <see cref="IFilter.AsOf"/> to reads of <see cref="IEntityAppendOnlyTrack"/> entities: when set,
/// the query is narrowed to the rows active at that instant via a plain (sargable) range predicate, which
/// composes with the rest of the read pipeline (column filters, ordering, paging). When AsOf is null the
/// query is untouched (full append-only history is returned). Mirrors <see cref="FilterConstraintHandler{T}"/>.
/// </summary>
public class AppendOnlyTrackReadingHandler<TEntity> : IEventHandler<EntityReadingEvent<TEntity>>
    where TEntity : class, IEntityAppendOnlyTrack
{
    public Task HandleAsync(EntityReadingEvent<TEntity> @event, CancellationToken token)
    {
        var asOf = @event?.Filter?.AsOf;
        if (@event?.Entities == null || asOf == null)
            return Task.CompletedTask;

        var at = asOf.Value;
        @event.Entities = @event.Entities.Where(e =>
            (e.ActiveFrom == null || e.ActiveFrom <= at) &&
            (e.ActiveTo == null || e.ActiveTo > at));

        return Task.CompletedTask;
    }
}
