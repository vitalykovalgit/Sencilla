
namespace Sencilla.Core
{
    /// <summary>
    /// 
    /// </summary>
    /// <typeparam name="TEntity"></typeparam>
    /// <typeparam name="TKey"></typeparam>
    public interface IRemoveRepository<TEntity, TKey> : IReadRepository<TEntity, TKey>
               where TEntity : IEntity<TKey>
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="ids"></param>
        /// <returns></returns>
        Task<TEntity> Undo(params TKey[] ids);

        /// <summary>
        /// 
        /// </summary>
        /// <param name="id"></param>
        /// <param name="token"></param>
        /// <returns></returns>
        Task<TEntity> Remove(IEnumerable<TKey> ids, CancellationToken? token = null);
	}
}
