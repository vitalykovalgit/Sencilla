
namespace Sencilla.Repository.EntityFramework;

/// <summary>
/// 
/// </summary>
/// <typeparam name="TEntity"></typeparam>
/// <typeparam name="TContext"></typeparam>
public class UpdateRepository<TEntity, TContext> : UpdateRepository<TEntity, TContext, int>, IUpdateRepository<TEntity>
    where TEntity : class, IEntity<int>, IEntityUpdateable, new()
    where TContext : DbContext
{
    public UpdateRepository(RepositoryDependency dependency, TContext context) : base(dependency, context) { }
}

/// <summary>
/// 
/// </summary>
/// <typeparam name="TEntity"></typeparam>
/// <typeparam name="TContext"></typeparam>
/// <typeparam name="TKey"></typeparam>
public class UpdateRepository<TEntity, TContext, TKey> : ReadRepository<TEntity, TContext, TKey>, IUpdateRepository<TEntity, TKey>
        where TEntity : class, IEntity<TKey>, IEntityUpdateable, new()
        where TContext : DbContext
{
    public UpdateRepository(RepositoryDependency dependency, TContext context) : base(dependency, context)
    {
    }

    public async Task<TEntity?> Update(TEntity entity, CancellationToken token = default)
    {
        return (await Update(new[] { entity })).FirstOrDefault();
    }

    public Task<IEnumerable<TEntity>> Update(params TEntity[] entities)
    {
        return Update(entities, CancellationToken.None);
    }

    public async Task<IEnumerable<TEntity>> Update(IEnumerable<TEntity> entities, CancellationToken token = default)
    {
        // Notify before updating 
        var eventUpdating = new EntityUpdatingEvent<TEntity> { Entities = entities.AsQueryable() };
        await D.Events.PublishAsync(eventUpdating);

        // TODO: Move to trackable 
        foreach (var e in entities)
        {
            if (e is IEntityUpdateableTrack)
                ((IEntityUpdateableTrack)e).UpdatedDate = DateTime.UtcNow;
        }

        DbContext.UpdateRange(entities);
        await Save(token);

        // Notify that entity updated
        var eventUpdated = new EntityUpdatedEvent<TEntity> { Entities = entities.AsQueryable() };
        await D.Events.PublishAsync(eventUpdated);

        return entities;
    }
}

