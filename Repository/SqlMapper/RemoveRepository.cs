using System;
using System.Threading;
using System.Threading.Tasks;

using Sencilla.Core.Entity;
using Sencilla.Core.Injection;
using Sencilla.Core.Repo;
using Sencilla.Infrastructure.SqlMapper.Impl;

namespace Sencilla.Impl.Repository.SqlMapper
{
    public class RemoveRepository<TEntity, TContext, TKey> : ReadRepository<TEntity, TContext, TKey>, IRemoveRepository<TEntity, TKey>
        where TEntity : class, IEntityRemoveable<TKey>, new()
        where TContext : DbContext
    {

        public RemoveRepository(IResolver resolver) : base(resolver)
        {
        }

        public TEntity Undo(TKey id)
        {
            using (var context = Resolve<TContext>())
            {
                var entity = context.Set<TEntity, TKey>().GetById(id);
                if (entity != null)
                {
                    entity.DeletedDate = null;
                    context.Set<TEntity, TKey>().Update(entity, id);

                    context.Commit();
                }
                return entity;
            }
        }

        public async Task<TEntity> UndoAsync(TKey id, CancellationToken? token = null)
        {
            using (var context = Resolve<TContext>())
            {
                var entity = await context.Set<TEntity, TKey>().GetByIdAsync(id, token);
                if (entity != null)
                {
                    entity.DeletedDate = null;
                    await context.Set<TEntity, TKey>().UpdateAsync(entity, id, token);

                    context.Commit();
                }
                return entity;
            }
        }

        public TEntity Remove(TKey id)
        {
            using (var context = Resolve<TContext>())
            {
                var entity = context.Set<TEntity, TKey>().GetById(id);
                if (entity != null)
                {
                    entity.DeletedDate = DateTime.UtcNow;
                    context.Set<TEntity, TKey>().Update(entity, id);

                    context.Commit();
                }
                return entity;
            }
        }

        public async Task<TEntity> RemoveAsync(TKey id, CancellationToken? token = null)
        {
            using (var context = Resolve<TContext>())
            {
                var entity = await context.Set<TEntity, TKey>().GetByIdAsync(id, token);
                if (entity != null)
                {
                    entity.DeletedDate = DateTime.UtcNow;
                    await context.Set<TEntity, TKey>().UpdateAsync(entity, id, token);

                    context.Commit();
                }
                return entity;
            }
        }
    }
}
