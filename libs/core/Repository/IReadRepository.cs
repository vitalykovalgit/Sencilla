namespace Sencilla.Core;

/// <summary>
/// 
/// </summary>
/// <typeparam name="TEntity"></typeparam>
public interface IReadRepository<TEntity> : IReadRepository<TEntity, int>
    where TEntity : IEntity<int>
{ 
}

/// <summary>
/// Base read entity interface
/// </summary>
/// <typeparam name="TEntity"></typeparam>
/// <typeparam name="TKey"></typeparam>
public interface IReadRepository<TEntity, TKey> : IBaseRepository
    where TEntity : IEntity<TKey>
{
    /// <summary>
    /// UNSAFE escape hatch: the raw store query with NO entity pipeline applied —
    /// permission constraints and filters are bypassed. For framework internals and
    /// deliberate system-level access only; application code should use
    /// <see cref="QueryAsync"/>.
    /// </summary>
    IQueryable<TEntity> Query { get; }

    /// <summary>
    /// Composable query with the entity reading pipeline applied (permission
    /// constraints, filters). Prefer this over <see cref="Query"/>.
    /// Default implementation falls back to the raw query for providers without
    /// an event pipeline (enforcement is EF-only).
    /// </summary>
    Task<IQueryable<TEntity>> QueryAsync(CancellationToken token = default) => Task.FromResult(Query);

    /// <summary>
    /// 
    /// </summary>
    /// <param name="id"></param>
    /// <param name="token"></param>
    /// <param name="includes"></param>
    /// <returns></returns>
    Task<TEntity?> GetById(TKey id, CancellationToken token = default, params Expression<Func<TEntity, object>>[]? with);
        
    /// <summary>
    /// 
    /// </summary>
    /// <param name="ids"></param>
    /// <param name="token"></param>
    /// <param name="includes"></param>
    /// <returns></returns>
    Task<IEnumerable<TEntity>> GetByIds(IEnumerable<TKey> ids, CancellationToken token = default, params Expression<Func<TEntity, object>>[]? with);

    /// <summary>
    /// 
    /// </summary>
    /// <param name="token"></param>
    /// <param name="includes"></param>
    /// <returns></returns>
    Task<IEnumerable<TEntity>> GetAll(IFilter? filter = null, CancellationToken token = default, params Expression<Func<TEntity, object>>[]? with);
        
    /// <summary>
    /// 
    /// </summary>
    /// <param name="filter"></param>
    /// <param name="token"></param>
    /// <param name="with"></param>
    /// <returns></returns>
    Task<TEntity?> FirstOrDefault(IFilter? filter = null, CancellationToken token = default, params Expression<Func<TEntity, object>>[]? with);

    /// <summary>
    /// Retrive count of the entities 
    /// </summary>
    /// <param name="token"></param>
    Task<int> GetCount(IFilter? filter = null, CancellationToken token = default);


    Task<object> GetSum(IFilter? filter = null, CancellationToken token = default);
    Task<object> GetMax(IFilter? filter = null, CancellationToken token = default);
    Task<object> GetMin(IFilter? filter = null, CancellationToken token = default);
    Task<double> GetAvarage(IFilter? filter = null, CancellationToken token = default);

    /// <summary>
    /// Begins a database transaction
    /// </summary>
    /// <param name="token"></param>
    /// <returns>Database transaction</returns>
    Task<IDbTransaction> BeginTransaction(CancellationToken token = default);

    /// <summary>
    /// UNSAFE: filters the raw <see cref="Query"/> — NO entity pipeline applied
    /// (permission constraints and filters are bypassed). For framework internals
    /// and deliberate system-level access only; application code should compose
    /// on <see cref="QueryAsync"/> instead.
    /// </summary>
    /// <param name="predicate">The filter predicate</param>
    /// <returns>Filtered queryable</returns>
    IQueryable<TEntity> Where(Expression<Func<TEntity, bool>> predicate);
}