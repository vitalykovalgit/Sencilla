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

    public async Task<int> Delete(IEnumerable<TKey> ids, CancellationToken token = default)
    {
        var count = await DbContext.Set<TEntity>().Where(e => ids.Contains(e.Id)).ExecuteDeleteAsync(token).ConfigureAwait(false);
        await Save(token);
        return count;
    }

    public Task<int> Delete(TEntity entity, CancellationToken token = default)
    {
        return Delete(new[] { entity }, token);
    }

    public async Task<int> Delete(IEnumerable<TEntity> entities, CancellationToken token = default)
    {
        // Add events

        DbContext.RemoveRange(entities);
        var count = await Save(token);

        return count;
    }
}
