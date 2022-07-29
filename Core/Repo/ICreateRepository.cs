
namespace Sencilla.Core
{
    /// <summary>
    /// 
    /// </summary>
    /// <typeparam name="TEntity"></typeparam>
    public interface ICreateRepository<TEntity> : ICreateRepository<TEntity, int>
           where TEntity : IEntity<int>
    { 
    }

    /// <summary>
    /// 
    /// </summary>
    /// <typeparam name="TEntity"></typeparam>
    /// <typeparam name="TKey"></typeparam>
    public interface ICreateRepository<TEntity, TKey> : IReadRepository<TEntity, TKey>
               where TEntity : IEntity<TKey>
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="entity"></param>
        /// <returns></returns>
        Task<TEntity> Create(TEntity entity, CancellationToken token = default);

        /// <summary>
        /// 
        /// </summary>
        /// <param name="entities"></param>
        /// <returns></returns>
        Task<IEnumerable<TEntity>> Create(IEnumerable<TEntity> entities, CancellationToken token = default);
               
    }
}
