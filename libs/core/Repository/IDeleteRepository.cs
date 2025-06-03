
namespace Sencilla.Core;

/// <summary>
/// 
/// </summary>
/// <typeparam name="TEntity"></typeparam>
/// <typeparam name="TKey"></typeparam>
public interface IDeleteRepository<TEntity> : IDeleteRepository<TEntity, int>
       where TEntity : IEntity<int>
{ }

/// <summary>
/// Items will be hard deleted 
/// </summary>
/// <typeparam name="TEntity"></typeparam>
/// <typeparam name="TKey"></typeparam>
public interface IDeleteRepository<TEntity, TKey> : IReadRepository<TEntity, TKey> 
           where TEntity : IEntity<TKey>
{
    /// <summary>
    /// 
    /// </summary>
    /// <param name="ids"></param>
    /// <returns></returns>
    Task<int> Delete(TKey id, CancellationToken token = default);
    Task<int> Delete(TEntity entity, CancellationToken token = default);

    /// <summary>
    /// 
    /// </summary>
    /// <param name="ids"></param>
    /// <returns></returns>
    Task<int> Delete(IEnumerable<TKey> ids, CancellationToken token = default);
    Task<int> Delete(IEnumerable<TEntity> ids, CancellationToken token = default);
}
