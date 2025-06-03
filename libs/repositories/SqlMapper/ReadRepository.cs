//using System;
//using System.Collections.Generic;
//using System.Linq.Expressions;
//using System.Threading;
//using System.Threading.Tasks;

//using Sencilla.Core.Entity;
//using Sencilla.Core.Injection;
//using Sencilla.Core.Repo;
//using Sencilla.Infrastructure.SqlMapper.Impl;

//namespace Sencilla.Impl.Repository.SqlMapper
//{
//    public class ReadRepository<TEntity, TContext, TKey> : BaseRepository<TContext>, IReadRepository<TEntity, TKey>
//           where TEntity : class, IEntity<TKey>, new()
//           where TContext : DbContext
//    {
//        public ReadRepository(IResolver resolver) : base(resolver)
//        {
//        }

//        public List<TEntity> GetAll()
//        {
//            using (var context = Resolve<TContext>())
//            {
//                return context.Set<TEntity, TKey>().GetAll();
//            }
//        }

//        public List<TEntity> GetAll(params Expression<Func<TEntity, object>>[] properties)
//        {
//            using (var context = Resolve<TContext>())
//            {
//                return context.Set<TEntity, TKey>().GetAll(properties);
//            }
//        }

//        public Task<List<TEntity>> GetAllAsync(CancellationToken? token = default(CancellationToken?))
//        {
//            using (var context = Resolve<TContext>())
//            {
//                return context.Set<TEntity, TKey>().GetAllAsync(token);
//            }
//        }

//        public TEntity GetById(TKey id, params Expression<Func<TEntity, object>>[] properties)
//        {
//            using (var context = Resolve<TContext>())
//            {
//                return context.Set<TEntity, TKey>().GetById(id, properties);
//            }
//        }

//        public Task<TEntity> GetByIdAsync(TKey id, params Expression<Func<TEntity, object>>[] properties)
//        {
//            using (var context = Resolve<TContext>())
//            {
//                return context.Set<TEntity, TKey>().GetByIdAsync(id, CancellationToken.None, properties);
//            }
//        }

//        public Task<TEntity> GetByIdAsync(TKey id, CancellationToken token, params Expression<Func<TEntity, object>>[] properties)
//        {
//            using (var context = Resolve<TContext>())
//            {
//                return context.Set<TEntity, TKey>().GetByIdAsync(id, token, properties);
//            }
//        }

//        public List<TEntity> GetMany(IEnumerable<TKey> ids, params Expression<Func<TEntity, object>>[] includes)
//        {
//            using (var context = Resolve<TContext>())
//            {
//                return context.Set<TEntity, TKey>().GetMany(ids, includes);
//            }
//        }
        
//        public int GetCount()
//        {
//            using (var context = Resolve<TContext>())
//            {
//                return context.Set<TEntity, TKey>().Count();
//            }
//        }

//        public Task<int> GetCountAsync(CancellationToken? token = default(CancellationToken?))
//        {
//            using (var context = Resolve<TContext>())
//            {
//                return context.Set<TEntity, TKey>().CountAsync(token);
//            }
//        }
//    }
//}
