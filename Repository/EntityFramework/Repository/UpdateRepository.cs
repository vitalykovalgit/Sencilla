using Microsoft.EntityFrameworkCore;
using Sencilla.Core;

namespace Sencilla.Repository.EntityFramework
{
    /// <summary>
    /// 
    /// </summary>
    /// <typeparam name="TEntity"></typeparam>
    /// <typeparam name="TContext"></typeparam>
    public class UpdateRepository<TEntity, TContext> : UpdateRepository<TEntity, TContext, int>, IUpdateRepository<TEntity>
       where TEntity : class, IEntity<int>, IEntityUpdateable, new()
       where TContext : DbContext
    {
        public UpdateRepository(IResolver resolver, TContext context) : base(resolver, context) { }
    }

    /// <summary>
    /// 
    /// </summary>
    /// <typeparam name="TEntity"></typeparam>
    /// <typeparam name="TContext"></typeparam>
    /// <typeparam name="TKey"></typeparam>
    public class UpdateRepository<TEntity, TContext, TKey> : ReadRepository<TEntity, TContext, TKey>, IUpdateRepository<TEntity, TKey>
           where TEntity : class, IEntity<TKey>, IEntityUpdateable, new()
           where TContext : DbContext
    {
        public UpdateRepository(IResolver resolver, TContext context) : base(resolver, context)
        {
        }

        public async Task<TEntity> Update(TEntity entity, CancellationToken token = default)
        {
            // Updated date 
            if (entity is IEntityUpdateableTrack)
               (entity as IEntityUpdateableTrack).UpdatedDate = DateTime.UtcNow;

            DbContext.Update(entity);
            await Save(token);

            return entity;
        }

        public async Task<IEnumerable<TEntity>> Update(IEnumerable<TEntity> entities, CancellationToken token = default)
        {
            foreach (var e in entities)
            {
                if (e is IEntityUpdateableTrack)
                   (e as IEntityUpdateableTrack).UpdatedDate = DateTime.UtcNow;
            }

            DbContext.UpdateRange(entities);
            await Save(token);
            
            return entities;
        }
    }
}
