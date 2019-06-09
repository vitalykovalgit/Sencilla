using System.Collections.Generic;
using System.Threading.Tasks;

namespace Sencilla.Mobile.Core
{
    /// <summary>
    /// 
    /// </summary>
    public interface IRepository<TEntity> where TEntity : class, IEntity, new()
    {
        Task<TEntity> Get(int id);

        Task<int> Count(Filter.Filter filter = null);
        Task<IEnumerable<TEntity>> GetAll(Filter.Filter filter = null);
        
        Task<TEntity> Create(TEntity entity);
        Task<TEntity> Update(TEntity entity);
        Task<TEntity> Remove(TEntity entity);
        Task<TEntity> Delete(TEntity entity);
    }

}
