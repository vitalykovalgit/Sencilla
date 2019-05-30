using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;

using Sencilla.Core.Entity;

namespace Sencilla.Core.Repo
{
    public interface IUpdateRepository<TEntity, TKey> : IReadRepository<TEntity, TKey>
               where TEntity : IEntity<TKey>
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="entity"></param>
        /// <param name="ruleSet"></param>
        /// <returns></returns>
        TEntity Update(TEntity entity, string ruleSet = null);

        /// <summary>
        /// 
        /// </summary>
        /// <param name="entity"></param>
        /// <param name="token"></param>
        /// <param name="ruleSet"></param>
        /// <returns></returns>
        Task<TEntity> UpdateAsync(TEntity entity, CancellationToken? token = null, string ruleSet = null);

        /// <summary>
        /// 
        /// </summary>
        /// <param name="entities"></param>
        /// <returns></returns>
        IEnumerable<TEntity> Update(IEnumerable<TEntity> entities);
        
        /// <summary>
        /// 
        /// </summary>
        /// <param name="entities"></param>
        /// <param name="token"></param>
        /// <returns></returns>
        Task<IEnumerable<TEntity>> UpdateAsync(IEnumerable<TEntity> entities, CancellationToken? token = null);
    }
}
