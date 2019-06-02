using Sencilla.Infrastructure.SqlMapper.Mapping;
using System.Collections.Generic;
using System.Data.Common;
using System.Threading;
using System.Threading.Tasks;

namespace Sencilla.Infrastructure.SqlMapper.Impl.Expression
{
    public abstract class BaseQueryBuilder : BaseExpression
    {
        /// <summary>
        /// List of parameters 
        /// </summary>
        public List<DbParameter> Parameters { get; } = new List<DbParameter>();

        /// <summary>
        /// Add parameters 
        /// </summary>
        /// <param name="parameters"></param>
        public void Add(params DbParameter[] parameters)
        {
            Parameters.AddRange(parameters);
        }


        public abstract void Add(string name, object value);
    }

    public class BaseQueryBuilder<TEntity> : BaseQueryBuilder, ISqlExpressionExecutable<TEntity>
    {
        /// <summary>
        /// Table mapping 
        /// </summary>
        protected ITableMapping<TEntity> Table { get; }

        /// <summary>
        /// Database Context 
        /// </summary>
        protected DbContext Context { get; }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="context"></param>
        /// <param name="table"></param>
        public BaseQueryBuilder(DbContext context, ITableMapping<TEntity> table)
        {
            Context = context;
            Table = table;
        }

        public override void Add(string name, object value)
        {
            Add(Context.DbProvider.Param.Create(name, value));
        }

        /// <inheritdoc />
        /// <summary>
        /// </summary>
        /// <returns></returns>
        public int Execute(QueryParams qp)
        {
            return Context.Connection.ExecuteQuery(Sql, Context.Transaction, ToParams(qp));
        }
        
        /// <inheritdoc />
        /// <summary>
        /// </summary>
        /// <param name="qp"></param>
        /// <param name="token"></param>
        /// <returns></returns>
        public Task<int> ExecuteAsync(QueryParams qp, CancellationToken? token = null)
        {
            return Context.Connection.ExecuteQueryAsync(Sql, Context.Transaction, token ?? CancellationToken.None, ToParams(qp));
        }

        /// <inheritdoc />
        /// <summary>
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public T ReadFirstColumn<T>()
        {
            return Context.Connection
                          .ExecuteQueryReader(Sql, Context.Transaction, ToParams(null))
                          .ReadFirstColumn<T>();
        }

        /// <inheritdoc />
        /// <summary>
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="token"></param>
        /// <returns></returns>
        public Task<T> ReadFirstColumnAsync<T>(CancellationToken? token = null)
        {
            return Context.Connection
                          .ExecuteQueryReaderAsync(Sql, Context.Transaction, token ?? CancellationToken.None, ToParams(null))
                          .ReadFirstColumnAsync<T>();
        }

        /// <inheritdoc />
        /// <summary>
        /// Retrive first or default value 
        /// </summary>
        /// <returns></returns>
        public TEntity FirstOrDefault(QueryParams qp)
        {
            return Context.Connection
                          .ExecuteQueryReader(Sql, Context.Transaction, ToParams(qp))
                          .ReadFirstOrDefault(Table);
        }

        /// <inheritdoc />
        /// <summary>
        /// </summary>
        /// <param name="qp"></param>
        /// <param name="token"></param>
        /// <returns></returns>
        public Task<TEntity> FirstOrDefaultAsync(QueryParams qp, CancellationToken ? token = null)
        {
            return Context.Connection
                          .ExecuteQueryReaderAsync(Sql, Context.Transaction, token ?? CancellationToken.None, ToParams(qp))
                          .ReadFirstOrDefaultAsync(Table);
        }

        /// <inheritdoc />
        /// <summary>
        /// </summary>
        /// <returns></returns>
        public List<TEntity> ToList(QueryParams qp)
        {
            return Context.Connection
                          .ExecuteQueryReader(Sql, Context.Transaction, ToParams(qp))
                          .ReadList(Table);
        }

        /// <inheritdoc />
        /// <summary>
        /// </summary>
        /// <param name="qp"></param>
        /// <param name="token"></param>
        /// <returns></returns>
        public Task<List<TEntity>> ToListAsync(QueryParams qp, CancellationToken? token = null)
        {
            return Context.Connection
                          .ExecuteQueryReaderAsync(Sql, Context.Transaction, token ?? CancellationToken.None, ToParams(qp))
                          .ReadListAsync(Table);
        }

        /// <summary>
        /// Utility 
        /// </summary>
        /// <param name="qp"></param>
        /// <returns></returns>
        private DbParameter[] ToParams(QueryParams qp)
        {
            if (qp?.Count > 0)
            {
                Parameters.AddRange(qp.ToArray(Context.DbProvider.Param));
            }

            return Parameters?.ToArray();
        }
    }
}
