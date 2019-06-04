using System;
using System.Threading;
using System.Threading.Tasks;

using Sencilla.Core.Entity;
using Sencilla.Core.Injection;
using Sencilla.Core.Repo;
using Sencilla.Infrastructure.SqlMapper.Impl;

namespace Sencilla.Impl.Repository.SqlMapper
{
    public class HideRepository<TEntity, TContext, TKey> : ReadRepository<TEntity, TContext, TKey>, IHideRepository<TEntity, TKey>
        where TEntity : class, IEntityHideable<TKey>, new()
        where TContext : DbContext
    {

        public HideRepository(IResolver resolver) : base(resolver)
        {
        }

        public TEntity Hide(TEntity entity)
        {
            throw new NotImplementedException();
        }

        public Task<TEntity> HideAsync(TEntity entity, CancellationToken? token = default(CancellationToken?))
        {
            throw new NotImplementedException();
        }

        public TEntity Show(TEntity entity)
        {
            throw new NotImplementedException();
        }

        public Task<TEntity> ShowAsync(TEntity entity, CancellationToken? token = default(CancellationToken?))
        {
            throw new NotImplementedException();
        }
    }
}
