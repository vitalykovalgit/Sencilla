namespace Sencilla.Repository.EntityFramework;

/// <summary>
/// 
/// </summary>
/// <typeparam name="TEntity"></typeparam>
/// <typeparam name="TContext"></typeparam>
public class ReadRepository<TEntity, TContext>(RepositoryDependency dependency, TContext context)
    : ReadRepository<TEntity, TContext, int>(dependency, context), IReadRepository<TEntity>
    where TEntity : class, IEntity<int>, new()
    where TContext : DbContext;

/// <summary>
/// 
/// </summary>
/// <typeparam name="TEntity"></typeparam>
/// <typeparam name="TContext"></typeparam>
/// <typeparam name="TKey"></typeparam>
public class ReadRepository<TEntity, TContext, TKey>(RepositoryDependency dependency, TContext context)
    : BaseRepository<TContext>(dependency, context), IReadRepository<TEntity, TKey>
    where TEntity : class, IEntity<TKey>, new()
    where TContext : DbContext
{
#pragma warning disable CS8602 // Dereference of a possibly null reference.
    public async Task<TEntity?> GetById(TKey id, CancellationToken token = default, params Expression<Func<TEntity, object>>[]? with)
    {
        var query = await Query(null, token);

        if (with is not null)
            foreach (var prop in with)
                query = query.Include(prop);

        return await query.FirstOrDefaultAsync(e => e.Id.Equals(id), token).ConfigureAwait(false);
    }
#pragma warning restore CS8602 // Dereference of a possibly null reference.

    public async Task<IEnumerable<TEntity>> GetByIds(IEnumerable<TKey> ids, CancellationToken token = default, params Expression<Func<TEntity, object>>[]? with)
    {
        var query = await Query(null, token);

        if (with is not null)
            foreach (var prop in with)
                query = query.Include(prop);

        return await query.Where(e => ids.Contains(e.Id)).ToListAsync(token).ConfigureAwait(false);
    }

    public async Task<IEnumerable<TEntity>> GetAll(IFilter? filter = null, CancellationToken token = default, params Expression<Func<TEntity, object>>[]? with)
    {
        var query = await Query(filter, token);

        if (with is not null)
            foreach (var prop in with)
                query = query.Include(prop);

        return await query.ToListAsync(token).ConfigureAwait(false);
    }

    public async Task<TEntity?> FirstOrDefault(IFilter? filter = null, CancellationToken token = default, params Expression<Func<TEntity, object>>[]? with)
    {
        var query = await Query(filter, token);

        if (with is not null)
            foreach (var prop in with)
                query = query.Include(prop);

        return await query.FirstOrDefaultAsync().ConfigureAwait(false);
    }


    public async Task<int> GetCount(IFilter? filter = null, CancellationToken token = default)
    {
        var query = await Query(filter, token);
        return await query.CountAsync(token);
    }

    public async Task<object> GetSum(IFilter? filter = null, CancellationToken token = default)
    {
        var query = await Query(filter, token);
        return query.Sum(filter?.Aggregate!);
    }

    public async Task<object> GetMax(IFilter? filter = null, CancellationToken token = default)
    {
        var query = await Query(filter, token);
        return query.Max(filter?.Aggregate!);
    }

    public async Task<object> GetMin(IFilter? filter = null, CancellationToken token = default)
    {
        var query = await Query(filter, token);
        return query.Max(filter?.Aggregate!);
    }

    public async Task<double> GetAvarage(IFilter? filter = null, CancellationToken token = default)
    {
        var query = await Query(filter, token);
        return query.Average(filter?.Aggregate!);
    }


    protected async Task<IQueryable<TEntity>> Query(IFilter? filter, CancellationToken token)
    {
        var e = new EntityReadingEvent<TEntity> 
        { 
            Filter = filter,
            Entities = DbContext.Query<TEntity>()
        };
        await D.Events.PublishAsync(e, token).ConfigureAwait(false);
        return e.Entities;
    }
}
