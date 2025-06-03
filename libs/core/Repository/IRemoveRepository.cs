
namespace Sencilla.Core;

/// <summary>
/// 
/// </summary>
/// <typeparam name="TEntity"></typeparam>
public interface IRemoveRepository<TEntity> : IRemoveRepository<TEntity, int>
           where TEntity : IEntity<int>
{ 
}

/// <summary>
/// 
/// </summary>
/// <typeparam name="TEntity"></typeparam>
/// <typeparam name="TKey"></typeparam>
public interface IRemoveRepository<TEntity, TKey> : IReadRepository<TEntity, TKey>
           where TEntity : IEntity<TKey>
{
    /// <summary>
    /// 
    /// </summary>
    /// <param name="ids"></param>
    /// <returns></returns>
    Task<TEntity?> Undo(TEntity entity, CancellationToken token = default);
    Task<IEnumerable<TEntity>> Undo(IEnumerable<TEntity> entities, CancellationToken token = default);

    /// <summary>
    /// 
    /// </summary>
    /// <param name="id"></param>
    /// <param name="token"></param>
    /// <returns></returns>
    Task<TEntity?> Remove(TEntity entity, CancellationToken token = default);
    Task<IEnumerable<TEntity>> Remove(IEnumerable<TEntity> entities, CancellationToken token = default);
}
