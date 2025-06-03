using Sencilla.Infrastructure.SqlMapper.Impl.Expression;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Sencilla.Infrastructure.SqlMapper
{
    /// <summary>
    /// Execute query and return result
    /// </summary>
    /// <typeparam name="TEntity"></typeparam>
    public interface ISqlExpressionExecutable<TEntity> : ISqlExpression
    {
        /// <summary>
        /// Just execute query and return number of affected records 
        /// </summary>
        /// <param name="qp"> Query parameters </param>
        /// <returns></returns>
        int Execute(QueryParams qp);

        /// <summary>
        /// Execute query asynchronously 
        /// </summary>
        /// <param name="qp"></param>
        /// <param name="token"></param>
        /// <returns></returns>
        Task<int> ExecuteAsync(QueryParams qp, CancellationToken? token = null);

        /// <summary>
        /// Reed first column from first record and convert it to type T
        /// If there is no record it reurns default(T)
        /// TODO; think about throwing an exception 
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        T ReadFirstColumn<T>();

        /// <summary>
        /// Read first column asynchronously (see <see cref="ReadFirstColumn"/>)
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="token"></param>
        /// <returns></returns>
        Task<T> ReadFirstColumnAsync<T>(CancellationToken? token = null);

        /// <summary>
        /// Read first record and cnvert it to TEntity 
        /// </summary>
        /// <param name="qp"></param>
        /// <returns></returns>
        TEntity FirstOrDefault(QueryParams qp);

        /// <summary>
        /// Read first record and cnvert it to TEntity asynchronoulsy 
        /// </summary>
        /// <param name="qp"></param>
        /// <param name="token"></param>
        /// <returns></returns>
        Task<TEntity> FirstOrDefaultAsync(QueryParams qp, CancellationToken? token = null);

        /// <summary>
        /// Read query and convert it to list of entities 
        /// </summary>
        /// <param name="qp"></param>
        /// <returns></returns>
        List<TEntity> ToList(QueryParams qp);

        /// <summary>
        /// Read query and convert it to list of entities 
        /// </summary>
        /// <param name="qp"></param>
        /// <param name="token"></param>
        /// <returns></returns>
        Task<List<TEntity>> ToListAsync(QueryParams qp, CancellationToken? token = null);
    }
}
