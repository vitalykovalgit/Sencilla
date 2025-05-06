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

    public async Task<IEnumerable<TEntity>> Remove(IEnumerable<TEntity> entities, CancellationToken token = default)
    {
        foreach(var e in entities)
            e.DeletedDate = DateTime.UtcNow;

        await Update(entities, token);
        return entities;
    }

    public async Task<TEntity?> Undo(TEntity entity, CancellationToken token = default)
    {
        return (await Undo([entity], token)).FirstOrDefault();
    }

    public async Task<IEnumerable<TEntity>> Undo(IEnumerable<TEntity> entities, CancellationToken token = default)
    {
        foreach (var e in entities)
            e.DeletedDate = null;

        await Update(entities, token);
        return entities;
    }
}
