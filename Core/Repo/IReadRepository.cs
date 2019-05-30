using System;
using System.Threading;
using System.Threading.Tasks;
using System.Linq.Expressions;
using System.Collections.Generic;

using Sencilla.Core.Entity;

namespace Sencilla.Core.Repo
{
    /// <summary>
    /// Base read entity interface
    /// </summary>
    /// <typeparam name="TEntity"></typeparam>
    /// <typeparam name="TKey"></typeparam>
    public interface IReadRepository<TEntity, TKey> : IBaseRepository
               where TEntity : IEntity<TKey>
    {
        /// <summary>
        /// Retrive count of the entities 
        /// </summary>
        int GetCount();

        /// <summary>
        /// Retrive count of the entities 
        /// </summary>
        /// <param name="token"></param>
        Task<int> GetCountAsync(CancellationToken? token = null);

        List<TEntity> GetAll();
        List<TEntity> GetAll(params Expression<Func<TEntity, object>>[] includes);
        Task<List<TEntity>> GetAllAsync(CancellationToken? token = null);

        TEntity GetById(TKey id, params Expression<Func<TEntity, object>>[] includes);
        Task<TEntity> GetByIdAsync(TKey id, params Expression<Func<TEntity, object>>[] includes);
        Task<TEntity> GetByIdAsync(TKey id, CancellationToken token, params Expression<Func<TEntity, object>>[] includes);

        List<TEntity> GetMany(IEnumerable<TKey> ids, params Expression<Func<TEntity, object>>[] includes);
    }
}
