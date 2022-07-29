
namespace Sencilla.Core
{
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
        Task<TEntity> Undo(TKey id, CancellationToken token = default);
        Task<TEntity> Undo(IEnumerable<TKey> ids, CancellationToken token = default);

        /// <summary>
        /// 
        /// </summary>
        /// <param name="id"></param>
        /// <param name="token"></param>
        /// <returns></returns>
        Task<TEntity> Remove(TKey id, CancellationToken token = default);

        /// <summary>
        /// 
        /// </summary>
        /// <param name="ids"></param>
        /// <param name="token"></param>
        /// <returns></returns>
        Task<TEntity> Remove(IEnumerable<TKey> ids, CancellationToken token = default);
    }
}
