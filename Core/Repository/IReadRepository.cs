using System.Linq.Expressions;

namespace Sencilla.Core
{
    /// <summary>
    /// 
    /// </summary>
    /// <typeparam name="TEntity"></typeparam>
    public interface IReadRepository<TEntity> : IReadRepository<TEntity, int>
           where TEntity : IEntity<int>
    { 
    }

    /// <summary>
    /// Base read entity interface
    /// </summary>
    /// <typeparam name="TEntity"></typeparam>
    /// <typeparam name="TKey"></typeparam>
    public interface IReadRepository<TEntity, TKey> : IBaseRepository
               where TEntity : IEntity<TKey>
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="id"></param>
        /// <param name="token"></param>
        /// <param name="includes"></param>
        /// <returns></returns>
        Task<TEntity?> GetById(TKey id, CancellationToken token = default, params Expression<Func<TEntity, object>>[] with);
        
        /// <summary>
        /// 
        /// </summary>
        /// <param name="ids"></param>
        /// <param name="token"></param>
        /// <param name="includes"></param>
        /// <returns></returns>
        Task<IEnumerable<TEntity>> GetByIds(IEnumerable<TKey> ids, CancellationToken token = default, params Expression<Func<TEntity, object>>[] with);

        /// <summary>
        /// 
        /// </summary>
        /// <param name="token"></param>
        /// <param name="includes"></param>
        /// <returns></returns>
        Task<IEnumerable<TEntity>> GetAll(IFilter? filter = null, CancellationToken token = default, params Expression<Func<TEntity, object>>[] with);
        
        /// <summary>
        /// 
        /// </summary>
        /// <param name="filter"></param>
        /// <param name="token"></param>
        /// <param name="with"></param>
        /// <returns></returns>
        Task<TEntity?> FirstOrDefault(IFilter? filter = null, CancellationToken token = default, params Expression<Func<TEntity, object>>[] with);

        /// <summary>
        /// Retrive count of the entities 
        /// </summary>
        /// <param name="token"></param>
        Task<int> GetCount(IFilter? filter = null, CancellationToken token = default);

    }
}
