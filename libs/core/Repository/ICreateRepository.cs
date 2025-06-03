namespace Sencilla.Core;

/// <summary>
/// 
/// </summary>
/// <typeparam name="TEntity"></typeparam>
public interface ICreateRepository<TEntity> : ICreateRepository<TEntity, int>
       where TEntity : IEntity<int>
{ 
}

/// <summary>
/// 
/// </summary>
/// <typeparam name="TEntity"></typeparam>
/// <typeparam name="TKey"></typeparam>
public interface ICreateRepository<TEntity, TKey> : IReadRepository<TEntity, TKey>
           where TEntity : IEntity<TKey>
{
    /// <summary>
    /// Create single entity async
    /// </summary>
    /// <param name="entity"></param>
    /// <param name="token"></param>
    /// <returns></returns>
    Task<TEntity?> Create(TEntity entity, CancellationToken token = default);

    /// <summary>
    /// Create many entities async
    /// </summary>
    /// <param name="entity"></param>
    /// <returns></returns>
    Task<IEnumerable<TEntity>> Create(params TEntity[] entities);

    /// <summary>
    /// Creates many entities async
    /// </summary>
    /// <param name="entities"></param>
    /// <returns></returns>
    Task<IEnumerable<TEntity>> Create(IEnumerable<TEntity> entities, CancellationToken token = default);

    /// <summary>
    /// Upsert single entity async
    /// </summary>
    /// <param name="entities"></param>
    /// <returns></returns>
    Task<TEntity> UpsertAsync(TEntity entity,
        Expression<Func<TEntity, object>> condition,
        Expression<Func<TEntity, TEntity>>? insertAction = null,
        Expression<Func<TEntity, TEntity>>? updateAction = null,
        CancellationToken token = default);

    /// <summary>
    /// Upsert many entities async
    /// </summary>
    /// <param name="entity"></param>
    /// <returns></returns>
    Task<IEnumerable<TEntity>> UpsertAsync(Expression<Func<TEntity, object>> condition,
        Expression<Func<TEntity, TEntity>>? insertAction = null,
        Expression<Func<TEntity, TEntity>>? updateAction = null,
        params TEntity[] entities);

    /// <summary>
    /// Upsert many entities async
    /// </summary>
    /// <param name="entities"></param>
    /// <returns></returns>
    Task<IEnumerable<TEntity>> UpsertAsync(IEnumerable<TEntity> entities,
        Expression<Func<TEntity, object>> condition,
        Expression<Func<TEntity, TEntity>>? insertAction = null,
        Expression<Func<TEntity, TEntity>>? updateAction = null,
        CancellationToken token = default);

    /// <summary>
    /// Merge single entity async (upsert and delete)
    /// </summary>
    /// <param name="entities"></param>
    /// <returns></returns>
    Task<TEntity> MergeAsync(TEntity entity,
        Expression<Func<TEntity, object>> condition,
        Expression<Func<TEntity, TEntity>>? insertAction = null,
        Expression<Func<TEntity, TEntity>>? updateAction = null,
        CancellationToken token = default);

    /// <summary>
    /// Merge many entities async (upsert and delete)
    /// </summary>
    /// <param name="entity"></param>
    /// <returns></returns>
    Task<IEnumerable<TEntity>> MergeAsync(Expression<Func<TEntity, object>> condition,
        Expression<Func<TEntity, TEntity>>? insertAction = null,
        Expression<Func<TEntity, TEntity>>? updateAction = null,
        params TEntity[] entities);

    /// <summary>
    /// Merge many entities async (upsert and delete)
    /// </summary>
    /// <param name="entities"></param>
    /// <returns></returns>
    Task<IEnumerable<TEntity>> MergeAsync(IEnumerable<TEntity> entities,
        Expression<Func<TEntity, object>> condition,
        Expression<Func<TEntity, TEntity>>? insertAction = null,
        Expression<Func<TEntity, TEntity>>? updateAction = null,
        CancellationToken token = default);
}
