using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;

using Sencilla.Core.Entity;

namespace Sencilla.Core.Repo
{
    public interface ICreateRepository<TEntity, TKey> : IReadRepository<TEntity, TKey>
               where TEntity : IEntity<TKey>
    {
        /// <summary>
        /// Create entity
        /// </summary>
        /// <param name="entity"></param>
        /// <returns></returns>
        TEntity Create(TEntity entity);
        
        /// <summary>
        /// 
        /// </summary>
        /// <param name="entity"></param>
        /// <returns></returns>
        Task<TEntity> CreateAsync(TEntity entity, CancellationToken? token = null);

        /// <summary>
        /// Create list of entities 
        /// </summary>
        /// <param name="entities"></param>
        /// <returns></returns>
        IEnumerable<TEntity> Create(IEnumerable<TEntity> entities);

        /// <summary>
        /// 
        /// </summary>
        /// <param name="entities"></param>
        /// <returns></returns>
        Task<IEnumerable<TEntity>> CreateAsync(IEnumerable<TEntity> entities, CancellationToken? token = null);
               
    }
}
