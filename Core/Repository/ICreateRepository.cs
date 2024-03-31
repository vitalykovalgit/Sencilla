
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
    /// <param name="entity"></param>
    /// <param name="token"></param>
    /// <returns></returns>
    Task<TEntity?> Upsert(TEntity entity,
        System.Linq.Expressions.Expression<Func<TEntity, object>> columnPrimaryKeyExpression, CancellationToken token = default);

    /// <summary>
    /// Upsert many entities async
    /// </summary>
    /// <param name="entity"></param>
    /// <returns></returns>
    Task<IEnumerable<TEntity>> Upsert(System.Linq.Expressions.Expression<Func<TEntity, object>> columnPrimaryKeyExpression, params TEntity[] entities);

    /// <summary>
    /// Upsert many entities async
    /// </summary>
    /// <param name="entities"></param>
    /// <returns></returns>
    Task<IEnumerable<TEntity>> Upsert(IEnumerable<TEntity> entities,
        System.Linq.Expressions.Expression<Func<TEntity, object>> columnPrimaryKeyExpression, CancellationToken token = default);
}
