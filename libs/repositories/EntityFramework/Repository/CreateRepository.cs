namespace Sencilla.Repository.EntityFramework;

/// <summary>
/// 
/// </summary>
/// <typeparam name="TEntity"></typeparam>
/// <typeparam name="TContext"></typeparam>
public class CreateRepository<TEntity, TContext> : CreateRepository<TEntity, TContext, int>, ICreateRepository<TEntity>
   where TEntity : class, IEntity<int>, IEntityCreateable, new()
   where TContext : DbContext
{
    public CreateRepository(RepositoryDependency dependency, TContext context): base(dependency, context) { }
}

/// <summary>
/// 
/// </summary>
/// <typeparam name="TEntity"></typeparam>
/// <typeparam name="TContext"></typeparam>
/// <typeparam name="TKey"></typeparam>
public class CreateRepository<TEntity, TContext, TKey> : ReadRepository<TEntity, TContext, TKey>, ICreateRepository<TEntity, TKey>
       where TEntity : class, IEntity<TKey>, IEntityCreateable, new()
       where TContext : DbContext
{
    public CreateRepository(RepositoryDependency dependency, TContext context): base(dependency, context)
    {
    }

    public async Task<TEntity?> Create(TEntity entity, CancellationToken token = default)
    {
        return (await Create(new[] { entity })).FirstOrDefault();
    }

    public Task<IEnumerable<TEntity>> Create(params TEntity[] entities)
    {
        return Create(entities, CancellationToken.None);
    }

    public async Task<IEnumerable<TEntity>> Create(IEnumerable<TEntity> entities, CancellationToken token = default)
    {
        // Check constraints 
        var eventCreating = new EntityCreatingEvent<TEntity> { Entities = entities.AsQueryable() };
        await D.Events.PublishAsync(eventCreating);

        // update creation date 
        // TODO: Move to event handler 
        foreach (var e in entities)
        {
            if (e is IEntityCreateableTrack)
               ((IEntityCreateableTrack)e).CreatedDate = DateTime.UtcNow;
        }

        // Add to context and save
        DbContext.AddRange(entities);
        await Save(token);

        // Notify about 
        var eventCreated = new EntityCreatedEvent<TEntity> { Entities = entities.AsQueryable() };
        await D.Events.PublishAsync(eventCreated);

        return entities;
    }

    public async Task<TEntity> UpsertAsync(TEntity entity,
        Expression<Func<TEntity, object>> condition,
        Expression<Func<TEntity, TEntity>>? insertAction = null,
        Expression<Func<TEntity, TEntity>>? updateAction = null,
        CancellationToken token = default)
    {
        await UpsertAsync([entity], condition, insertAction, updateAction, token);

        return entity;
    }

    public Task<IEnumerable<TEntity>> UpsertAsync(Expression<Func<TEntity, object>> condition,
        Expression<Func<TEntity, TEntity>>? insertAction = null,
        Expression<Func<TEntity, TEntity>>? updateAction = null,
        params TEntity[] entities) => UpsertAsync(entities, condition, insertAction, updateAction);

    public async Task<IEnumerable<TEntity>> UpsertAsync(IEnumerable<TEntity> entities,
        Expression<Func<TEntity, object>> condition,
        Expression<Func<TEntity, TEntity>>? insertAction = null,
        Expression<Func<TEntity, TEntity>>? updateAction = null,
        CancellationToken token = default)
    {
        // Check constraints 
        var eventCreating = new EntityCreatingEvent<TEntity> { Entities = entities.AsQueryable() };
        await D.Events.PublishAsync(eventCreating);

        // update creation date
        // TODO: Move to event handler 
        foreach (var e in entities)
        {
            if (e is IEntityCreateableTrack)
                ((IEntityCreateableTrack)e).CreatedDate = DateTime.UtcNow;
        }

        await DbContext.UpsertBulkAsync(entities, condition, insertAction, updateAction);

        // Notify about 
        var eventCreated = new EntityCreatedEvent<TEntity> { Entities = entities.AsQueryable() };
        await D.Events.PublishAsync(eventCreated);

        // TODO: Think about returning values
        return entities;
    }

    public async Task<TEntity> MergeAsync(TEntity entity,
        Expression<Func<TEntity, object>> condition,
        Expression<Func<TEntity, TEntity>>? insertAction = null,
        Expression<Func<TEntity, TEntity>>? updateAction = null,
        CancellationToken token = default)
    {
        await MergeAsync([entity], condition, insertAction, updateAction, token);

        return entity;
    }

    public Task<IEnumerable<TEntity>> MergeAsync(Expression<Func<TEntity, object>> condition,
        Expression<Func<TEntity, TEntity>>? insertAction = null,
        Expression<Func<TEntity, TEntity>>? updateAction = null,
        params TEntity[] entities) => MergeAsync(entities, condition, insertAction, updateAction);

    public async Task<IEnumerable<TEntity>> MergeAsync(IEnumerable<TEntity> entities,
        Expression<Func<TEntity, object>> condition,
        Expression<Func<TEntity, TEntity>>? insertAction = null,
        Expression<Func<TEntity, TEntity>>? updateAction = null,
        CancellationToken token = default)
    {
        // TODO: replace creating by merging
        var eventCreating = new EntityCreatingEvent<TEntity> { Entities = entities.AsQueryable() };
        await D.Events.PublishAsync(eventCreating);

        // update creation date
        // TODO: Move to event handler
        if (entities.Any())
            foreach (var e in entities)
                if (e is IEntityCreateableTrack)
                    ((IEntityCreateableTrack)e).CreatedDate = DateTime.UtcNow;

        await DbContext.MergeBulkAsync(entities, condition, insertAction, updateAction);

        // TODO: replace created by merged
        var eventCreated = new EntityCreatedEvent<TEntity> { Entities = entities.AsQueryable() };
        await D.Events.PublishAsync(eventCreated);

        // TODO: Think about returning values
        return entities;
    }
}
