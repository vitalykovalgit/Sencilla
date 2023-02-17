
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
}
