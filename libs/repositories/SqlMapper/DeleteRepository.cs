//using System.Linq;
//using System.Threading;
//using System.Threading.Tasks;
//using System.Collections.Generic;
//using Sencilla.Infrastructure.SqlMapper.Impl;
//using Sencilla.Core.Entity;
//using Sencilla.Core.Repo;
//using Sencilla.Core.Injection;

//namespace Sencilla.Impl.Repository.SqlMapper
//{
//    public class DeleteRepository<TEntity, TContext, TKey> : ReadRepository<TEntity, TContext, TKey>, IDeleteRepository<TEntity, TKey>
//        where TEntity : class, IEntityDeleteable<TKey>, new()
//        where TContext : DbContext
//    {
//        //protected IDeleteObserver<TEntity, TKey>[] observers;

//        public DeleteRepository(IServiceProvider resolver /*IDeleteObserver<TEntity, TKey>[] observers*/) : base(resolver)
//        {
//            //this.observers = observers;
//        }

//        protected bool HasObservers => false;

//        public TEntity Delete(params TKey[] ids)
//        {
//            IList<TEntity> items = HasObservers ? GetMany(ids) : null;
//            OnDeleting(items);
//            using (var context = Resolve<TContext>())
//            {
//                context.Set<TEntity, TKey>().DeleteMany(ids);
//                context.Commit();
//            }

//            OnDeleted(items);

//            return null;
//        }

//        public async Task<TEntity> DeleteAsync(CancellationToken? token = null, params TKey[] ids)
//        {
//            IList<TEntity> items = HasObservers ? GetMany(ids) : null;

//            OnDeleting(items);

//            using (var context = Resolve<TContext>())
//            {
//                foreach (var id in ids)
//                {
//                    // TODO: Make it more fast by using only one sql command 
//                    await context.Set<TEntity, TKey>().DeleteAsync(id, token);
//                }
//                context.Commit();
//            }

//            OnDeleted(items);

//            return null;
//        }

//        private void OnDeleted(IEnumerable<TEntity> entities)
//        {
//            //if (entities != null && entities.Any())
//            //{
//            //    observers?.ToList().ForEach(p => p.OnDeleted(entities));
//            //}
//        }

//        private void OnDeleting(IEnumerable<TEntity> entities)
//        {
//            //TODO need to implement in observer
//        }
//    }
//}
