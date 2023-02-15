﻿using Z.EntityFramework.Plus;

namespace Sencilla.Repository.EntityFramework
{
    public class DeleteRepository<TEntity, TContext> : DeleteRepository<TEntity, TContext, int>, IDeleteRepository<TEntity>
       where TEntity : class, IEntity<int>, IEntityDeleteable, new()
       where TContext : DbContext
    {
        public DeleteRepository(RepositoryDependency dependency, TContext context) : base(dependency, context) { }
    }

    /// <summary>
    /// 
    /// </summary>
    /// <typeparam name="TEntity"></typeparam>
    /// <typeparam name="TContext"></typeparam>
    /// <typeparam name="TKey"></typeparam>
    public class DeleteRepository<TEntity, TContext, TKey> : ReadRepository<TEntity, TContext, TKey>, IDeleteRepository<TEntity, TKey>
           where TEntity : class, IEntity<TKey>, IEntityDeleteable, new()
           where TContext : DbContext
    {
        public DeleteRepository(RepositoryDependency dependency, TContext context): base(dependency, context)
        {
        }

#pragma warning disable CS8602 // Dereference of a possibly null reference.
        public async Task<int> Delete(TKey id, CancellationToken token = default)
        {
            var entity = DbContext.Set<TEntity>().FirstOrDefault(e => e.Id.Equals(id));
            if (entity != null)
            {
                DbContext.Remove(entity);
                return await Save(token);
            }

            return 0;
        }
#pragma warning restore CS8602 // Dereference of a possibly null reference.

        public async Task<int> Delete(TEntity entity, CancellationToken token = default)
        {
            DbContext.Remove(entity);
            var count = await Save(token);
            return count;
        }

        public async Task<int> Delete(IEnumerable<TKey> ids, CancellationToken token = default)
        {
            var count = await DbContext.Set<TEntity>().Where(e => ids.Contains(e.Id)).DeleteAsync(token);
            await Save(token);
            return count;
        }

        public async Task<int> Delete(IEnumerable<TEntity> entities, CancellationToken token = default)
        {
            DbContext.RemoveRange(entities);
            var count = await Save(token);
            return count;
        }
    }
}
