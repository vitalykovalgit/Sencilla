using Sencilla.Infrastracture.SqlMapper.Impl;
using Sencilla.Infrastructure.SqlMapper.Contract;
using Sencilla.Infrastructure.SqlMapper.Impl.Expression;
using Sencilla.Infrastructure.SqlMapper.Mapping;
using Sencilla.Infrastructure.SqlMapper.Mapping.Extention;
using Sencilla.Infrastructure.SqlMapper.Mapping.Impl.Table;
using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Linq;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;

namespace Sencilla.Infrastructure.SqlMapper.Impl.Set
{
    internal abstract class DbSet<TEntity, TKey> : ISet<TEntity, TKey> where TEntity : class, new()
    {
        protected DbContext Context { get; }

        protected ITableMapping<TEntity> Table { get; }

        protected DbSet(DbContext context)
        {
            Context = context;

            // TODO: Take from cache 
            //Table = new TableMapping<>();

            Table = TableMappingCache.Instance.GetTableMapping<TEntity>();
        }

        /// <inheritdoc />
        /// <summary>
        /// </summary>
        /// <returns></returns>
        public abstract int Count();
        public abstract Task<int> CountAsync(CancellationToken? token = null);
        public abstract int Count(IFilter filter);

        /// <inheritdoc />
        /// <summary>
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public abstract TEntity GetById(TKey id, params Expression<Func<TEntity, object>>[] properties);
        public abstract Task<TEntity> GetByIdAsync(TKey id, CancellationToken? token = null, params Expression<Func<TEntity, object>>[] properties);

        /// <inheritdoc />
        /// <summary>
        /// </summary>
        /// <returns></returns>
        public abstract List<TEntity> GetAll(params Expression<Func<TEntity, object>>[] properties);
        public abstract Task<List<TEntity>> GetAllAsync(CancellationToken? token = null, params Expression<Func<TEntity, object>>[] properties);
        public abstract List<TEntity> GetMany(IEnumerable<TKey> ids, params Expression<Func<TEntity, object>>[] properties);
        public abstract List<TEntity> GetAll(IFilter filter, params Expression<Func<TEntity, object>>[] includes);

        /// <inheritdoc />
        /// <summary>
        /// Retuns primary key inserted 
        /// </summary>
        /// <param name="entity"></param>
        /// <returns></returns>
        public abstract TKey Insert(TEntity entity);
        public abstract Task<TKey> InsertAsync(TEntity entity, CancellationToken? token = null);

        /// <inheritdoc />
        /// <summary>
        /// Retuns number of row affected
        /// </summary>
        /// <param name="entity"></param>
        /// <param name="id"></param>
        /// <returns></returns>
        public abstract int Update(TEntity entity, TKey id);
        public abstract Task<int> UpdateAsync(TEntity entity, TKey id, CancellationToken? token = null);

        /// <inheritdoc />
        /// <summary>
        /// Delete entity from DB
        /// </summary>
        /// <param name="id"></param>
        /// <returns>Number of rows affected</returns>
        public abstract int Delete(TKey id);        
        public abstract Task<int> DeleteAsync(TKey id, CancellationToken? token = null);
        public abstract int DeleteMany(params TKey[] id);

        /// <inheritdoc />
        /// <summary>
        /// </summary>
        /// <returns></returns>
        public abstract int DeleteAll();
        public abstract Task<int> DeleteAllAsync(CancellationToken? token = null);

        /// <inheritdoc />
        /// <summary>
        /// Expressions
        /// </summary>
        /// <returns></returns>
        public abstract SelectQueryBuilder<TEntity> Select();
        public abstract UpdateQueryBuilder<TEntity> Update();
        public abstract InsertQueryBuilder<TEntity> Insert();
        public abstract DeleteQueryBuilder<TEntity> Delete();

        public int RunStoredProcedure(string name, params DbParam[] parameters)
        {
            return Context.ExecuteProcedure(Parse(name), Params(parameters));
        }

        public Task<int> RunStoredProcedureAsync(string name, CancellationToken? token = null, params DbParam[] parameters)
        {
            return Context.ExecuteProcedureAsync(Parse(name), token ?? CancellationToken.None, Params(parameters));
        }

        public List<TEntity> ReadStoredProcedure(string name, params DbParam[] parameters)
        {
             return Context.ExecuteProcedureReader(Parse(name), Params(parameters)).ReadList(Table);
        }

        public Task<List<TEntity>> ReadStoredProcedureAsync(string name, CancellationToken? token = null, params DbParam[] parameters)
        {
            return Context.ExecuteProcedureReaderAsync(Parse(name), token, Params(parameters)).ReadListAsync(Table, token);
        }
        
        public TEntity ReadFirstStoredProcedure(string name, params DbParam[] parameters)
        {
            return Context.ExecuteProcedureReader(Parse(name), Params(parameters)).ReadFirstOrDefault(Table);
        }

        public Task<TEntity> ReadFirstStoredProcedureAsync(string name, CancellationToken? token = null, params DbParam[] parameters)
        {
            return Context.ExecuteProcedureReaderAsync(Parse(name), token, Params(parameters)).ReadFirstOrDefaultAsync(Table, token);
        }

        public IList<Include> Includes(params Expression<Func<TEntity, object>>[] properties)
        {
            var converter = new IncludeConverter<TEntity>();

            return converter.ToIncludes(Table, properties);
        }

        #region Implementation 

        protected DbParameter[] Params(params DbParam[] parameters)
        {
            return parameters.Select(p => p.ToParameter(Context.DbProvider.Param)).ToArray();
        }

        /// <summary>
        /// Returns list of db params, for <paramref name="entity"/> except it's primary key field.
        /// </summary>
        protected DbParameter[] InsertEntityParams(TEntity entity)
        {
            return Table.WriteColumns.ToParams(Context.DbProvider.Param, entity).ToArray();
        }

        /// <summary>
        /// Returns list of db params, for <paramref name="entity"/>.
        /// </summary>
        protected DbParameter[] UpdateEntityParams(TEntity entity)
        {
            return Table.Columns.ToParams(column => !column.IsRowVersion, Context.DbProvider.Param, entity).ToArray();
        }

        protected string Parse(string name)
        {
            return name.Replace(Markers.Schema, Table.Schema).Replace(Markers.Table, Table.Name);
        }

        #endregion 

    }
}
