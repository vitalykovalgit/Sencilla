namespace Sencilla.Repository.EntityFramework;

public class DeleteRepository<TEntity, TContext> : DeleteRepository<TEntity, TContext, int>, IDeleteRepository<TEntity>
   where TEntity : class, IEntity<int>, IEntityDeleteable, new()
   where TContext : DbContext
{
    public DeleteRepository(RepositoryDependency dependency, TContext context) : base(dependency, context) { }
}

/// <summary>
/// 
/// </summary>
/// <typeparam name="TEntity"></typeparam>
/// <typeparam name="TContext"></typeparam>
/// <typeparam name="TKey"></typeparam>
public class DeleteRepository<TEntity, TContext, TKey> : ReadRepository<TEntity, TContext, TKey>, IDeleteRepository<TEntity, TKey>
       where TEntity : class, IEntity<TKey>, IEntityDeleteable, new()
       where TContext : DbContext
{
    public DeleteRepository(RepositoryDependency dependency, TContext context): base(dependency, context)
    {
    }

    public Task<int> Delete(TKey id, CancellationToken token = default)
    {
        return Delete(new[] { id }, token);
    }

    public Task<int> Delete(IEnumerable<TKey> ids, CancellationToken token = default)
    {
        return InTransaction(async () =>
        {
            var dbQuery = DbContext.Set<TEntity>().Where(e => ids.Contains(e.Id));

            var eventDeleting = new EntityDeletingEvent<TEntity> { Entities = dbQuery };
            await D.Events.PublishAsync(eventDeleting, token);
            await ThrowIfNarrowed(eventDeleting.Entities, dbQuery, token);

            // Delete through the narrowed query — composed constraints become part of
            // the DELETE's WHERE, so a row can never slip past the check.
            var count = await (eventDeleting.Entities ?? dbQuery).ExecuteDeleteAsync(token).ConfigureAwait(false);
            await Save(token);

            // No payload: the rows are gone and were never materialized.
            await D.Events.PublishAsync(new EntityDeletedEvent<TEntity>(), token);

            return count;
        }, token);
    }
    public Task<int> Delete(TEntity entity, CancellationToken token = default)
    {
        return Delete(new[] { entity }, token);
    }

    public Task<int> Delete(IEnumerable<TEntity> entities, CancellationToken token = default)
    {
        return InTransaction(async () =>
        {
            // Constraints check the rows as they exist in the DB (pre-image),
            // not the client-supplied objects.
            var ids = entities.Select(e => e.Id).ToList();
            var dbQuery = DbContext.Set<TEntity>().AsNoTracking().Where(e => ids.Contains(e.Id));

            var eventDeleting = new EntityDeletingEvent<TEntity> { Entities = dbQuery };
            await D.Events.PublishAsync(eventDeleting, token);
            await ThrowIfNarrowed(eventDeleting.Entities, dbQuery, token);

            DbContext.RemoveRange(entities);

            // Idempotent delete: a row already removed (or its key never persisted) since the client
            // loaded it surfaces as an EF concurrency violation — the DELETE affects 0 rows. Deleting
            // a missing row is a success, not a 500: detach the missed entries and retry the rest.
            // Terminates: every iteration detaches at least one entry.
            while (true)
            {
                try
                {
                    var count = await Save(token);
                    await D.Events.PublishAsync(new EntityDeletedEvent<TEntity> { Entities = entities.AsQueryable() }, token);
                    return count;
                }
                catch (DbUpdateConcurrencyException e)
                {
                    if (!e.Entries.Any())
                        throw;

                    foreach (var entry in e.Entries)
                        entry.State = EntityState.Detached;
                }
            }
        }, token);
    }
}
