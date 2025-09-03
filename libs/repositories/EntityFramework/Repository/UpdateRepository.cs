﻿
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

    public Task<IEnumerable<TEntity>> Update(params TEntity[] entities)
    {
        return Update(entities, CancellationToken.None);
    }

    public async Task<IEnumerable<TEntity>> Update(IEnumerable<TEntity> entities, CancellationToken token = default)
    {
        // Notify before updating 
        var query = entities.AsQueryable();
        var eventUpdating = new EntityUpdatingEvent<TEntity> { Entities = query };
        await D.Events.PublishAsync(eventUpdating, token);

        // TODO: Move to trackable 
        foreach (var e in query)
        {
            if (e is IEntityUpdateableTrack track)
                track.UpdatedDate = DateTime.UtcNow;
        }

        DbContext.UpdateRange(query);
        await Save(token);

        // Notify that entity updated
        var eventUpdated = new EntityUpdatedEvent<TEntity> { Entities = query };
        await D.Events.PublishAsync(eventUpdated, token);

        return query;
    }
}

