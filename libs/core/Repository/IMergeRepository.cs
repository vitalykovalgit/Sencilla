namespace Sencilla.Core;

/// <summary>
/// 
/// </summary>
/// <typeparam name="TEntity"></typeparam>
public interface IMergeRepository<TEntity> : IMergeRepository<TEntity, int>
       where TEntity : IEntity<int>
{
}

/// <summary>
/// Atomically merges a single entry into a JSON dictionary property of an entity
/// without read-modify-write, preserving concurrent writes from other callers.
/// </summary>
/// <typeparam name="TEntity"></typeparam>
/// <typeparam name="TKey"></typeparam>
public interface IMergeRepository<TEntity, TKey> : IReadRepository<TEntity, TKey>
           where TEntity : IEntity<TKey>
{
    /// <summary>
    /// Atomically sets <paramref name="key"/> -> <paramref name="value"/> on the
    /// JSON dictionary property selected by <paramref name="property"/> for the
    /// entity identified by <paramref name="id"/>.
    /// </summary>
    Task<int> MergeAsync<TValue>(TKey id, Expression<Func<TEntity, IDictionary<string, TValue>?>> property,
        string key, TValue value, CancellationToken token = default);
}
