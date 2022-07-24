
namespace Sencilla.Core
{
    public interface IUpdateRepository<TEntity, TKey> : IReadRepository<TEntity, TKey>
               where TEntity : IEntity<TKey>
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="entities"></param>
        /// <returns></returns>
        Task<TEntity> Update(params TEntity[] entities);
        
        /// <summary>
        /// 
        /// </summary>
        /// <param name="entities"></param>
        /// <param name="token"></param>
        /// <returns></returns>
        Task<IEnumerable<TEntity>> UpdateAsync(IEnumerable<TEntity> entities, CancellationToken? token = null);
    }
}
