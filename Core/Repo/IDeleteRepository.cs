
namespace Sencilla.Core
{
    /// <summary>
    /// 
    /// </summary>
    /// <typeparam name="TEntity"></typeparam>
    /// <typeparam name="TKey"></typeparam>
    public interface IDeleteRepository<TEntity> : IDeleteRepository<TEntity, int>
           where TEntity : IEntity<int>
    { }

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
        /// <param name="ids"></param>
        /// <returns></returns>
        Task<TEntity> Delete(TKey id, CancellationToken token = default);
        Task<TEntity> Delete(TEntity id, CancellationToken token = default);

        /// <summary>
        /// 
        /// </summary>
        /// <param name="ids"></param>
        /// <returns></returns>
        Task<TEntity> Delete(IEnumerable<TKey> ids, CancellationToken token = default);
        Task<TEntity> Delete(IEnumerable<TEntity> ids, CancellationToken token = default);
    }
}
