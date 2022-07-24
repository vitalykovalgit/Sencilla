
namespace Sencilla.Core
{
    /// <summary>
    /// 
    /// </summary>
    /// <typeparam name="TEntity"></typeparam>
    /// <typeparam name="TKey"></typeparam>
    public interface IHideRepository<TEntity, TKey> : IReadRepository<TEntity, TKey>
               where TEntity : IEntity<TKey>
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="entity"></param>
        /// <param name="token"></param>
        /// <returns></returns>
        Task<TEntity> Hide(TEntity entity, CancellationToken? token = null);

        /// <summary>
        /// 
        /// </summary>
        /// <param name="entity"></param>
        /// <param name="token"></param>
        /// <returns></returns>
        Task<TEntity> Show(TEntity entity, CancellationToken? token = null);
    }
}
