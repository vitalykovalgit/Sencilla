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
        // Materialise once: a lazy IEnumerable yields fresh instances on every pass, so what a handler stamps, what
        // is created below and what is saved and returned would be different objects.
        var rows = entities as IReadOnlyList<TEntity> ?? entities.ToList();

        return InTransaction<IEnumerable<TEntity>>(async () =>
        {
            var query = rows.AsQueryable();

            // Pre-image: constraints are evaluated against current DB state, never
            // against the client-supplied objects (those can be forged to satisfy any
            // rule while targeting someone else's row). Handlers narrow DbEntities;
            // losing an existing row denies the whole batch.
            var ids = rows.Select(e => e.Id).ToList();
            var dbQuery = DbContext.Set<TEntity>().AsNoTracking().Where(e => ids.Contains(e.Id));

            // Every row posted, the ones about to be created too, so a handler can prepare those.
            var eventUpdating = new EntityUpdatingEvent<TEntity> { Entities = query, DbEntities = dbQuery };
            await D.Events.PublishAsync(eventUpdating, token);
            await ThrowIfNarrowed(eventUpdating.DbEntities, dbQuery, token);

            // A row whose generated key is unset matches neither image, and UpdateRange would INSERT it: any Update
            // grant would add any row, past Create and its constraints. It is a create, so the create repository
            // makes it, judged and announced as every create is, and Updated does not report it again. An entity
            // with no create repository cannot gain a row here. Split first: a created row has its key after.
            var byKey = rows.ToLookup(e => DbContext.Entry(e).IsKeySet);
            var created = byKey[false].ToList();
            var updated = byKey[true].ToList();
            if (created.Count > 0)
                await (R<ICreateRepository<TEntity, TKey>>() ?? throw new ForbiddenException($"{typeof(TEntity).Name} cannot be created."))
                    .Create(created, token);

            await PersistUpdate(updated, token);

            // Post-image: the same constraints must still hold after the write (a row
            // may not be moved out of the writer's permission scope). Read inside the
            // ambient transaction; a violation throws and the write rolls back.
            var eventUpdated = new EntityUpdatedEvent<TEntity> { Entities = updated.AsQueryable(), DbEntities = dbQuery };
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

        DbContext.UpdateRange(entities);

        try
        {
            await Save(token);
        }
        catch (DbUpdateConcurrencyException e)
        {
            // "Expected 1 row, affected 0" — the entities here are the caller's own objects, attached
            // detached and marked Modified without ever being loaded, so the UPDATE keys on whatever
            // id arrived. Unlike a delete, where a row already gone is a SUCCESS (DeleteRepository
            // detaches those entries and retries), an update must never absorb it: the caller's change
            // would silently vanish. Left unhandled this is a 500 for what the caller can fix by
            // re-reading, so it becomes a 409 instead.
            throw Conflict(e);
        }

        context.ChangeTracker.Clear();
    }

    /// <summary>
    /// Names which of the two causes produced the 0-row update. Only an entity that declares a
    /// concurrency token (<c>[Timestamp]</c> / <c>IsConcurrencyToken()</c>) can lose the race on a
    /// row that still exists; without one the predicate is the key alone, so 0 rows can only mean the
    /// id is not in the table.
    /// </summary>
    private static ConflictException Conflict(DbUpdateConcurrencyException e)
    {
        var versioned = e.Entries.Any(entry => entry.Metadata.GetProperties().Any(p => p.IsConcurrencyToken));

        return new ConflictException(versioned
            ? $"{typeof(TEntity).Name} changed since it was read; re-read it and apply the update again."
            : $"{typeof(TEntity).Name} no longer exists; re-read before updating it.");
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

