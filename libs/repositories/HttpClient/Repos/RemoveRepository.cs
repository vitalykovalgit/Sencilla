//using System;
//using System.Threading;
//using System.Threading.Tasks;

//using Sencilla.Core.Entity;
//using Sencilla.Core.Injection;
//using Sencilla.Core.Repo;

//namespace Sencilla.Impl.Repository.HttpClient
//{
//    public class RemoveRepository<TContext, TEntity, TKey> : ReadRepository<TContext, TEntity, TKey>, IRemoveRepository<TEntity, TKey>
//        where TEntity : class, IEntity<TKey>, new()
//        where TContext : WebContext
//    {
//        public RemoveRepository(IResolver resolver) : base(resolver)
//        {
//        }

//        public TEntity Remove(TKey id)
//        {
//            throw new NotImplementedException();
//        }

//        public Task<TEntity> RemoveAsync(TKey id, CancellationToken? token = null)
//        {
//            throw new NotImplementedException();
//        }

//        public TEntity Undo(TKey id)
//        {
//            throw new NotImplementedException();
//        }

//        public Task<TEntity> UndoAsync(TKey id, CancellationToken? token = null)
//        {
//            throw new NotImplementedException();
//        }
//    }
//}
