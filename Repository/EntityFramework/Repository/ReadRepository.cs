namespace Sencilla.Repository.EntityFramework;

/// <summary>
/// 
/// </summary>
/// <typeparam name="TEntity"></typeparam>
/// <typeparam name="TContext"></typeparam>
public class ReadRepository<TEntity, TContext> : ReadRepository<TEntity, TContext, int>, IReadRepository<TEntity>
   where TEntity : class, IEntity<int>, new()
   where TContext : DbContext
{
    public ReadRepository(RepositoryDependency dependency, TContext context) : base(dependency, context) { }
}

/// <summary>
/// 
/// </summary>
/// <typeparam name="TEntity"></typeparam>
/// <typeparam name="TContext"></typeparam>
/// <typeparam name="TKey"></typeparam>
public class ReadRepository<TEntity, TContext, TKey> : BaseRepository<TContext>, IReadRepository<TEntity, TKey>
       where TEntity : class, IEntity<TKey>, new()
       where TContext : DbContext
{
    public ReadRepository(RepositoryDependency dependency, TContext context) : base(dependency, context)
    {
    }

#pragma warning disable CS8602 // Dereference of a possibly null reference.
    public async Task<TEntity?> GetById(TKey id, CancellationToken token = default, params Expression<Func<TEntity, object>>[] with)
    {
        var query = await Query(null);

        if (with is not null)
            foreach (var prop in with)
                query = query.Include(prop);

        return await query.FirstOrDefaultAsync(e => e.Id.Equals(id), token).ConfigureAwait(false);
    }
#pragma warning restore CS8602 // Dereference of a possibly null reference.

    public async Task<IEnumerable<TEntity>> GetByIds(IEnumerable<TKey> ids, CancellationToken token = default, params Expression<Func<TEntity, object>>[] with)
    {
        var query = await Query(null);

        if (with is not null)
            foreach (var prop in with)
                query = query.Include(prop);

        return await query.Where(e => ids.Contains(e.Id)).ToListAsync(token).ConfigureAwait(false);
    }

    public async Task<IEnumerable<TEntity>> GetAll(IFilter? filter = null, CancellationToken token = default, params Expression<Func<TEntity, object>>[] with)
    {
        var query = await Query(filter);

        if (with is not null)
            foreach (var prop in with)
                query = query.Include(prop);

        return await query.ToListAsync(token).ConfigureAwait(false);
    }

    public async Task<int> GetCount(IFilter? filter = null, CancellationToken token = default)
    {
        var query = await Query(filter);
        return await query.CountAsync(token);
    }

    protected async Task<IQueryable<TEntity>> Query(IFilter? filter)
    {
        var e = new EntityReadingEvent<TEntity> 
        { 
            Filter = filter,
            Entities = DbContext.Query<TEntity>().AsNoTracking()
        };
        await D.Events.PublishAsync(e).ConfigureAwait(false);
        return e.Entities;
    }
}
