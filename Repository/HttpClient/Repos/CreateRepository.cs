//using System;
//using System.Collections.Generic;
//using System.Threading;
//using System.Threading.Tasks;
//using Sencilla.Core;
//using Sencilla.Core.Repo;

//namespace Sencilla.Impl.Repository.HttpClient
//{
//    public class CreateRepository<TContext, TEntity, TKey> : ReadRepository<TContext, TEntity, TKey>, ICreateRepository<TEntity, TKey>
//        where TEntity : class, IEntity<TKey>, new()
//        where TContext : WebContext
//    {
//        public CreateRepository(IResolver resolver) : base(resolver)
//        {
//        }

//        public TEntity Create(TEntity entity)
//        {
//            throw new NotImplementedException();
//        }

//        public IEnumerable<TEntity> Create(IEnumerable<TEntity> entities)
//        {
//            throw new NotImplementedException();
//        }

//        public Task<TEntity> CreateAsync(TEntity entity, CancellationToken? token = null)
//        {
//            throw new NotImplementedException();
//        }

//        public Task<IEnumerable<TEntity>> CreateAsync(IEnumerable<TEntity> entities, CancellationToken? token = null)
//        {
//            throw new NotImplementedException();
//        }
//    }
//}
