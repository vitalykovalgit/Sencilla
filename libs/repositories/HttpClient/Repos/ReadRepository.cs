//using System.Threading;
//using System.Threading.Tasks;
//using System.Collections.Generic;

//using Sencilla.Core;
//using Sencilla.Core.Repo;

//namespace Sencilla.Impl.Repository.HttpClient
//{
//    public class ReadRepository<TContext, TEntity, TKey> : BaseRepository, IReadRepository<TEntity, TKey>
//        where TEntity : class, IEntity<TKey>, new()
//        where TContext: WebContext
//    {
//        public ReadRepository(IServiceProvider resolver) : base(resolver)
//        {
//        }

//        public List<TEntity> GetAll()
//        {
//            return GetAllAsync().Result;
//        }

//        public List<TEntity> GetAll(params System.Linq.Expressions.Expression<System.Func<TEntity, object>>[] includes)
//        {
//            return GetAllAsync().Result;
//        }

//        public Task<List<TEntity>> GetAllAsync(CancellationToken? token = null)
//        {
//            var context = R<TContext>();
//            var entities = GetAsync<List<TEntity>>(context.GetAllPath<TEntity>());
//            return entities;
//        }

//        public TEntity GetById(TKey id, params System.Linq.Expressions.Expression<System.Func<TEntity, object>>[] includes)
//        {
//            return GetByIdAsync(id, includes).Result;
//        }

//        public Task<TEntity> GetByIdAsync(TKey id, params System.Linq.Expressions.Expression<System.Func<TEntity, object>>[] includes)
//        {
//            return GetByIdAsync(id, CancellationToken.None, includes);
//        }

//        public async Task<TEntity> GetByIdAsync(TKey id, CancellationToken token, params System.Linq.Expressions.Expression<System.Func<TEntity, object>>[] includes)
//        {
//            var context = R<TContext>();
//            var entity = await GetAsync<TEntity>(context.GetByIdPath<TEntity, TKey>(id), token);
//            return entity;
//        }

//        public int GetCount()
//        {
//            return GetCountAsync().Result;
//        }

//        public async Task<int> GetCountAsync(CancellationToken? token = null)
//        {
//            var context = R<TContext>();
//            var count = await GetAsync<int>(context.GetCountPath<TEntity>(), token);
//            return count;
//        }

//        public List<TEntity> GetMany(IEnumerable<TKey> ids, params System.Linq.Expressions.Expression<System.Func<TEntity, object>>[] includes)
//        {
//            throw new System.NotSupportedException();
//        }

//        public void Save()
//        {
//            throw new System.NotSupportedException();
//        }
//    }
//}
