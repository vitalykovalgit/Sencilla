namespace Sencilla.Repository.EntityFramework;

/// <summary>
/// 
/// </summary>
/// <typeparam name="TEntity"></typeparam>
/// <typeparam name="TContext"></typeparam>
public class RemoveRepository<TEntity, TContext>(RepositoryDependency dependency, TContext context)
    : RemoveRepository<TEntity, TContext, int>(dependency, context), IRemoveRepository<TEntity>
    where TEntity : class, IEntity<int>, IEntityRemoveable, new()
    where TContext : DbContext;

/// <summary>
/// 
/// </summary>
public class RemoveRepository<TEntity, TContext, TKey>(RepositoryDependency dependency, TContext context)
    : UpdateRepository<TEntity, TContext, TKey>(dependency, context), IRemoveRepository<TEntity, TKey>
    where TEntity : class, IEntity<TKey>, IEntityRemoveable, new()
    where TContext : DbContext
{
    public async Task<TEntity?> Remove(TEntity entity, CancellationToken token = default)
    {
        return (await Remove([entity], token)).FirstOrDefault();
    }

    public Task<IEnumerable<TEntity>> Remove(IEnumerable<TEntity> entities, CancellationToken token = default)
    {
        return SetRemovedState(entities, DateTime.UtcNow, token);
    }

    public async Task<TEntity?> Undo(TEntity entity, CancellationToken token = default)
    {
        return (await Undo([entity], token)).FirstOrDefault();
    }

    public Task<IEnumerable<TEntity>> Undo(IEnumerable<TEntity> entities, CancellationToken token = default)
    {
        return SetRemovedState(entities, null, token);
    }

    /// <summary>
    /// Soft-delete and its undo are governed by the DELETE permission — mapped by
    /// intent, not implementation. They publish deleting/deleted events (constraints
    /// compose into the pre-image query) and persist through the update core without
    /// firing update events, so an update-only role cannot remove or resurrect rows.
    /// </summary>
    private Task<IEnumerable<TEntity>> SetRemovedState(IEnumerable<TEntity> entities, DateTime? deletedDate, CancellationToken token)
    {
        return InTransaction<IEnumerable<TEntity>>(async () =>
        {
            var ids = entities.Select(e => e.Id).ToList();
            var dbQuery = DbContext.Set<TEntity>().AsNoTracking().Where(e => ids.Contains(e.Id));

            var eventDeleting = new EntityDeletingEvent<TEntity> { Entities = dbQuery };
            await D.Events.PublishAsync(eventDeleting, token);
            await ThrowIfNarrowed(eventDeleting.Entities, dbQuery, token);

            foreach (var e in entities)
                e.DeletedDate = deletedDate;

            await PersistUpdate(entities, token);

            await D.Events.PublishAsync(new EntityDeletedEvent<TEntity> { Entities = entities.AsQueryable() }, token);

            return entities;
        }, token);
    }
}
