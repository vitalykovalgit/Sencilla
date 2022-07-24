
namespace Sencilla.Core
{
    public interface ICreateRepository<TEntity, TKey> : IReadRepository<TEntity, TKey>
               where TEntity : IEntity<TKey>
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="entity"></param>
        /// <returns></returns>
        Task<TEntity> Create(TEntity entity, CancellationToken? token = null);

        /// <summary>
        /// 
        /// </summary>
        /// <param name="entities"></param>
        /// <returns></returns>
        Task<IEnumerable<TEntity>> Create(IEnumerable<TEntity> entities, CancellationToken? token = null);
               
    }
}
