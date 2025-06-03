using Sencilla.Infrastructure.SqlMapper.Impl.Expression;
using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;

namespace Sencilla.Infrastructure.SqlMapper.Contract
{
    /// <summary>
    /// 
    /// </summary>
    /// <typeparam name="TEntity"></typeparam>
    /// <typeparam name="TKey"></typeparam>
    public interface ISet<TEntity, TKey>
    {
        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        int Count();
        int Count(IFilter filter);
        Task<int> CountAsync(CancellationToken? token = null);

        /// <summary>
        /// 
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        TEntity GetById(TKey id, params Expression<Func<TEntity, object>>[] properties);
        Task<TEntity> GetByIdAsync(TKey id, CancellationToken? token = null, params Expression<Func<TEntity, object>>[] properties);

        List<TEntity> GetMany(IEnumerable<TKey> ids, params Expression<Func<TEntity, object>>[] properties);

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        List<TEntity> GetAll(params Expression<Func<TEntity, object>>[] properties);
        List<TEntity> GetAll(IFilter filter, params Expression<Func<TEntity, object>>[] includes);
        Task<List<TEntity>> GetAllAsync(CancellationToken? token = null, params Expression<Func<TEntity, object>>[] properties);

        /// <summary>
        /// Retuns primary key inserted 
        /// </summary>
        /// <param name="entity"></param>
        /// <returns></returns>
        TKey Insert(TEntity entity);
        Task<TKey> InsertAsync(TEntity entity, CancellationToken? token = null);

        /// <summary>
        /// Retuns number of row affected
        /// </summary>
        /// <param name="entity"></param>
        /// <param name="id"></param>
        /// <returns></returns>
        int Update(TEntity entity, TKey id);
        Task<int> UpdateAsync(TEntity entity, TKey id, CancellationToken? token = null);

        /// <summary>
        /// Delete entity from DB
        /// </summary>
        /// <param name="id"></param>
        /// <returns>Number of rows affected</returns>
        int Delete(TKey id);
        Task<int> DeleteAsync(TKey id, CancellationToken? token = null);

        int DeleteMany(params TKey[] ids);

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        int DeleteAll();
        Task<int> DeleteAllAsync(CancellationToken? token = null);

        /// <summary>
        /// Returns how many records affected
        /// </summary>
        int RunStoredProcedure(string name, params DbParam[] parameters);

        /// <summary>
        /// Returns how many records affected
        /// </summary>
        Task<int> RunStoredProcedureAsync(string name, CancellationToken? token = null, params DbParam[] parameters);

        List<TEntity> ReadStoredProcedure(string name, params DbParam[] parameters);
        Task<List<TEntity>> ReadStoredProcedureAsync(string name, CancellationToken? token = null, params DbParam[] parameters);
        
        TEntity ReadFirstStoredProcedure(string name, params DbParam[] parameters);
        Task<TEntity> ReadFirstStoredProcedureAsync(string name, CancellationToken? token = null, params DbParam[] parameters);

        /// <summary>
        /// Expressions 
        /// </summary>
        /// <returns></returns>
        SelectQueryBuilder<TEntity> Select();
        UpdateQueryBuilder<TEntity> Update();
        InsertQueryBuilder<TEntity> Insert();
        DeleteQueryBuilder<TEntity> Delete();

        IList<Include> Includes(params Expression<Func<TEntity, object>>[] properties);
    }
}
