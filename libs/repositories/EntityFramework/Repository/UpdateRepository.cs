namespace Sencilla.Repository.EntityFramework;

/// <summary>
/// 
/// </summary>
/// <typeparam name="TEntity"></typeparam>
/// <typeparam name="TContext"></typeparam>
public class UpdateRepository<TEntity, TContext>(RepositoryDependency dependency, TContext context)
    : UpdateRepository<TEntity, TContext, int>(dependency, context), IUpdateRepository<TEntity>
    where TEntity : class, IEntity<int>, IEntityUpdateable, new()
    where TContext : DbContext;

/// <summary>
/// 
/// </summary>
/// <typeparam name="TEntity"></typeparam>
/// <typeparam name="TContext"></typeparam>
/// <typeparam name="TKey"></typeparam>
public class UpdateRepository<TEntity, TContext, TKey>(RepositoryDependency dependency, TContext context)
    : ReadRepository<TEntity, TContext, TKey>(dependency, context), IUpdateRepository<TEntity, TKey>
    where TEntity : class, IEntity<TKey>, IEntityUpdateable, new()
    where TContext : DbContext
{
    public async Task<TEntity?> Update(TEntity entity, CancellationToken token = default)
    {
        return (await Update([entity])).FirstOrDefault();
    }

    //public Task<int> ExecuteUpdateAsync(Action<IQueryable<TEntity>> updator, CancellationToken token = default)
    //{
    //    var query = this.DbContext.Query<TEntity>();
    //    updator(query);
    //    return query.Where(e => e != null).ExecuteUpdateAsync(s => s.SetProperty(e => e. = ), token);
    //}

    public Task<IEnumerable<TEntity>> Update(params TEntity[] entities)
    {
        return Update(entities, CancellationToken.None);
    }

    public void Detach(IEnumerable<TEntity> entities)
    {
        // Detach entities from context to avoid tracking issues
        foreach (var entity in entities) 
        {
            if (entity != null)
                DbContext.Entry(entity).State = EntityState.Detached;
        }
    }

    public void ClearChangeTracker()
    {
        DbContext.ChangeTracker.Clear();
    }

    public Task<IEnumerable<TEntity>> Update(IEnumerable<TEntity> entities, CancellationToken token = default)
    {
        return InTransaction<IEnumerable<TEntity>>(async () =>
        {
            var query = entities.AsQueryable();

            // Pre-image: constraints are evaluated against current DB state, never
            // against the client-supplied objects (those can be forged to satisfy any
            // rule while targeting someone else's row). Handlers narrow DbEntities;
            // losing an existing row denies the whole batch.
            var ids = entities.Select(e => e.Id).ToList();
            var dbQuery = DbContext.Set<TEntity>().AsNoTracking().Where(e => ids.Contains(e.Id));

            var eventUpdating = new EntityUpdatingEvent<TEntity> { Entities = query, DbEntities = dbQuery };
            await D.Events.PublishAsync(eventUpdating, token);
            await ThrowIfNarrowed(eventUpdating.DbEntities, dbQuery, token);

            await PersistUpdate(entities, token);

            // Post-image: the same constraints must still hold after the write (a row
            // may not be moved out of the writer's permission scope). Read inside the
            // ambient transaction; a violation throws and the write rolls back.
            var eventUpdated = new EntityUpdatedEvent<TEntity> { Entities = query, DbEntities = dbQuery };
            await D.Events.PublishAsync(eventUpdated, token);
            await ThrowIfNarrowed(eventUpdated.DbEntities, dbQuery, token);

            return query;
        }, token);
    }

    /// <summary>
    /// The persistence core of an update, shared with soft-delete (RemoveRepository),
    /// which runs the same write under Delete-action events instead of Update ones.
    /// </summary>
    protected async Task PersistUpdate(IEnumerable<TEntity> entities, CancellationToken token)
    {
        // TODO: Move to trackable
        foreach (var e in entities)
        {
            if (e is IEntityUpdateableTrack track)
                track.UpdatedDate = DateTime.UtcNow;
        }

        //try
        //{
            DbContext.UpdateRange(entities);
            await Save(token);
            context.ChangeTracker.Clear();
        //}
        //finally
        //{
            // Do not do like this!!!
            // context.ChangeTracker.Clear();
        //}
    }

    public Task<int> JsonMergeAsync<TValue>(TKey id,Expression<Func<TEntity, IDictionary<string, TValue>?>> property, string key, TValue value, CancellationToken token = default)
    {
        return InTransaction(async () =>
        {
            // A JSON patch is an UPDATE: pre-image constraint on the target row,
            // post-image re-check inside the ambient transaction.
            var ids = new[] { id };
            var dbQuery = DbContext.Set<TEntity>().AsNoTracking().Where(e => ids.Contains(e.Id));

            var eventUpdating = new EntityUpdatingEvent<TEntity> { DbEntities = dbQuery };
            await D.Events.PublishAsync(eventUpdating, token);
            await ThrowIfNarrowed(eventUpdating.DbEntities, dbQuery, token);

            var count = await DbContext.JsonMergeAsync(id, property, key, value, token);

            var eventUpdated = new EntityUpdatedEvent<TEntity> { DbEntities = dbQuery };
            await D.Events.PublishAsync(eventUpdated, token);
            await ThrowIfNarrowed(eventUpdated.DbEntities, dbQuery, token);

            return count;
        }, token);
    }
}

