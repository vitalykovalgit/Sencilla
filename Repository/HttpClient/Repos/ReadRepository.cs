using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

using Sencilla.Core.Entity;
using Sencilla.Core.Repo;

namespace Sencilla.Impl.Repository.HttpClient
{
    public class ReadRepository<TEntity, TKey> : BaseRepository, IReadRepository<TEntity, TKey>
        where TEntity : class, IEntity<TKey>, new()
    {
        public List<TEntity> GetAll()
        {
            return GetAllAsync().Result;
        }

        public List<TEntity> GetAll(params System.Linq.Expressions.Expression<System.Func<TEntity, object>>[] includes)
        {
            throw new System.NotSupportedException();
        }

        public Task<List<TEntity>> GetAllAsync(CancellationToken? token = null)
        {
            var entities = GetAsync<List<TEntity>>("");
            return entities;
        }

        /// <summary>
        /// Include is ignored in this implementation
        /// </summary>
        public TEntity GetById(TKey id, params System.Linq.Expressions.Expression<System.Func<TEntity, object>>[] includes)
        {
            return GetByIdAsync(id, includes).Result;
        }

        public Task<TEntity> GetByIdAsync(TKey id, params System.Linq.Expressions.Expression<System.Func<TEntity, object>>[] includes)
        {
            return GetByIdAsync(id, CancellationToken.None, includes);
        }

        public async Task<TEntity> GetByIdAsync(TKey id, CancellationToken token, params System.Linq.Expressions.Expression<System.Func<TEntity, object>>[] includes)
        {
            var entity = await GetAsync<TEntity>("", token);
            return entity;
        }

        public int GetCount()
        {
            return GetCountAsync().Result;
        }

        public async Task<int> GetCountAsync(CancellationToken? token = null)
        {
            var count = await GetAsync<int>("", token);
            return count;
        }

        public List<TEntity> GetMany(IEnumerable<TKey> ids, params System.Linq.Expressions.Expression<System.Func<TEntity, object>>[] includes)
        {
            throw new System.NotSupportedException();
        }

        public void Save()
        {
            throw new System.NotSupportedException();
        }
    }
}
