namespace Sencilla.Component.Tags;

/// <summary>
/// Sweeps a hard-deleted row's tags out of the shared table, which has no foreign key to cascade with. Hooks
/// <see cref="EntityDeletingEvent{TEntity}"/> rather than the -ed event on purpose: the -ed event is published
/// without entities on the bulk (ExecuteDelete) path, so pre-delete is the only point where the doomed ids are
/// reliably knowable — the same seam the audit component uses for deletes.
///
/// <para>Soft-deleted rows keep their tags: they are updates, not deletes, and never reach this handler.</para>
/// </summary>
public class SharedTagCleanupHandler<TEntity, TKey>(TagSharedRepository<TEntity, TKey> repository) : IEventHandler<EntityDeletingEvent<TEntity>>
    where TEntity : class, IEntity<TKey>, IEntityTaggableShared, new()
{
    public async Task HandleAsync(EntityDeletingEvent<TEntity> @event, CancellationToken token)
    {
        if (@event.Entities == null)
            return;

        // ponytail: synchronous materialisation. Queryable.ToList keeps this component free of an EF
        // dependency, and hard delete is a rare admin path, not a request hot path. Swap for ToListAsync if
        // Tags ever takes an EF reference for other reasons.
        var ids = @event.Entities.Select(e => e.Id).Distinct().ToList();

        await repository.Clear(ids, token);
    }
}
