using System.Threading;
using System.Threading.Tasks;
using Sencilla.Core.Entity;

namespace Sencilla.Core.Repo
{
    /// <summary>
    /// Items will be hard deleted 
    /// </summary>
    /// <typeparam name="TEntity"></typeparam>
    /// <typeparam name="TKey"></typeparam>
    public interface IDeleteRepository<TEntity, TKey> : IReadRepository<TEntity, TKey> 
               where TEntity : IEntity<TKey>
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        TEntity Delete(params TKey[] id);

        /// <summary>
        /// 
        /// </summary>
        /// <param name="token"></param>
        /// <param name="id"></param>
        /// <returns></returns>
        Task<TEntity> DeleteAsync(CancellationToken? token = null, params TKey[] id);
        
    }
}
