using System;
using System.Threading;
using System.Threading.Tasks;

using Sencilla.Core.Entity;
using Sencilla.Core.Injection;
using Sencilla.Core.Repo;

namespace Sencilla.Impl.Repository.HttpClient
{
    public class DeleteRepository<TContext, TEntity, TKey> : ReadRepository<TContext, TEntity, TKey>, IDeleteRepository<TEntity, TKey>
        where TEntity : class, IEntity<TKey>, new()
        where TContext : WebContext
    {
        public DeleteRepository(IResolver resolver) : base(resolver)
        {
        }

        public TEntity Delete(params TKey[] id)
        {
            throw new NotImplementedException();
        }

        public Task<TEntity> DeleteAsync(CancellationToken? token = null, params TKey[] id)
        {
            throw new NotImplementedException();
        }
    }
}
