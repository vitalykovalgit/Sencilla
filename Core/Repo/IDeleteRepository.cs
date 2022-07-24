
namespace Sencilla.Core
{
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
        Task<TEntity> Delete(params TKey[] ids);
        
        /// <summary>
        /// 
        /// </summary>
        /// <param name="ids"></param>
        /// <returns></returns>
        Task<TEntity> Delete(IEnumerable<TKey> ids, CancellationToken? token = null);
    }
}
