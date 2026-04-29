
namespace Sencilla.Core;

/// <summary>
/// 
/// </summary>
/// <typeparam name="TEntity"></typeparam>
/// <typeparam name="TKey"></typeparam>
public interface IUpdateRepository<TEntity> : IUpdateRepository<TEntity, int>
       where TEntity : IEntity<int>
{ 
}

/// <summary>
/// 
/// </summary>
/// <typeparam name="TEntity"></typeparam>
/// <typeparam name="TKey"></typeparam>
public interface IUpdateRepository<TEntity, TKey> : IReadRepository<TEntity, TKey>
           where TEntity : IEntity<TKey>
{
    /// <summary>
    /// 
    /// </summary>
    /// <param name="entity"></param>
    /// <returns></returns>
    Task<TEntity?> Update(TEntity entity, CancellationToken token = default);

    Task<IEnumerable<TEntity>> Update(params TEntity[] entities);

    /// <summary>
    /// 
    /// </summary>
    /// <param name="entities"></param>
    /// <param name="token"></param>
    /// <returns></returns>
    Task<IEnumerable<TEntity>> Update(IEnumerable<TEntity> entities, CancellationToken token = default);

    void Detach(IEnumerable<TEntity> entities);
    void ClearChangeTracker();

    /// <summary>
    /// Atomically sets <paramref name="key"/> -> <paramref name="value"/> on the JSON
    /// dictionary property selected by <paramref name="property"/> for the entity
    /// identified by <paramref name="id"/>, without read-modify-write. Concurrent
    /// merges to other keys of the same property are preserved.
    /// </summary>
    Task<int> JsonMergeAsync<TValue>(TKey id, Expression<Func<TEntity, IDictionary<string, TValue>?>> property,
        string key, TValue value, CancellationToken token = default);

}
