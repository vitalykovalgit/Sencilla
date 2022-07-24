using System.Linq.Expressions;

namespace Sencilla.Core
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
        /// <param name="token"></param>
        Task<int> GetCount(CancellationToken token = default);

        Task<IEnumerable<TEntity>> GetAll(CancellationToken token = default);

        Task<TEntity?> GetById(TKey id, params Expression<Func<TEntity, object>>[] includes);
        Task<TEntity?> GetById(TKey id, CancellationToken token, params Expression<Func<TEntity, object>>[] includes);

        Task<IEnumerable<TEntity>> GetMany(IEnumerable<TKey> ids, params Expression<Func<TEntity, object>>[] includes);
    }
}
