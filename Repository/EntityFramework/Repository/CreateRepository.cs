﻿using Sencilla.Repository.EntityFramework.Extension;

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

    public async Task UpsertAsync(TEntity entity, Expression<Func<TEntity, object>> condition,
        Expression<Func<TEntity, TEntity>>? insertAction = null,
        Expression<Func<TEntity, TEntity>>? updateAction = null,
        CancellationToken token = default)
    {
        // Check constraints 
        var eventCreating = new EntityCreatingEvent<TEntity> { Entities = new List<TEntity>() { entity }.AsQueryable() };
        await D.Events.PublishAsync(eventCreating);

        // update creation date 
        // TODO: Move to event handler 
        if (entity is IEntityCreateableTrack)
            ((IEntityCreateableTrack)entity).CreatedDate = DateTime.UtcNow;

        await DbContext.UpsertAsync(entity, condition, insertAction, updateAction);

        // Notify about 
        var eventCreated = new EntityCreatedEvent<TEntity> { Entities = new List<TEntity>() { entity }.AsQueryable() };
        await D.Events.PublishAsync(eventCreated);
    }
}
