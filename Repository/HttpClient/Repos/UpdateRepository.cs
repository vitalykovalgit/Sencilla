//using System;
//using System.Collections.Generic;
//using System.Threading;
//using System.Threading.Tasks;

//using Sencilla.Core.Entity;
//using Sencilla.Core.Injection;
//using Sencilla.Core.Repo;

//namespace Sencilla.Impl.Repository.HttpClient
//{
//    public class UpdateRepository<TContext, TEntity, TKey> : ReadRepository<TContext, TEntity, TKey>, IUpdateRepository<TEntity, TKey>
//        where TEntity : class, IEntity<TKey>, new()
//        where TContext : WebContext
//    {
//        public UpdateRepository(IResolver resolver) : base(resolver)
//        {
//        }

//        public TEntity Update(TEntity entity, string ruleSet = null)
//        {
//            throw new NotImplementedException();
//        }

//        public IEnumerable<TEntity> Update(IEnumerable<TEntity> entities)
//        {
//            throw new NotImplementedException();
//        }

//        public Task<TEntity> UpdateAsync(TEntity entity, CancellationToken? token = null, string ruleSet = null)
//        {
//            throw new NotImplementedException();
//        }

//        public Task<IEnumerable<TEntity>> UpdateAsync(IEnumerable<TEntity> entities, CancellationToken? token = null)
//        {
//            throw new NotImplementedException();
//        }
//    }
//}
