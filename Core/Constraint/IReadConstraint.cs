
namespace Sencilla.Core
{
    /// <summary>
    /// 
    /// </summary>
    public interface IReadConstraint
    {
        /// <summary>
        /// 
        /// </summary>
        /// <typeparam name="TEntity"></typeparam>
        /// <param name="query"></param>
        /// <param name="action"></param>
        /// <param name="filter"></param>
        /// <returns></returns>
        Task<IQueryable<TEntity>> Apply<TEntity>(IQueryable<TEntity> query, int action, IFilter? filter = null);
    }

}
