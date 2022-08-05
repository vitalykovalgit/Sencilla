using Microsoft.EntityFrameworkCore;
using Sencilla.Core;

namespace Sencilla.Repository.EntityFramework
{
    /// <summary>
    /// 
    /// </summary>
    /// <typeparam name="TEntity"></typeparam>
    /// <typeparam name="TContext"></typeparam>
    public class CreateRepository<TEntity, TContext> : CreateRepository<TEntity, TContext, int>, ICreateRepository<TEntity>
       where TEntity : class, IEntity<int>, IEntityCreateable, new()
       where TContext : DbContext
    {
        public CreateRepository(IResolver resolver, TContext context): base(resolver, context){ }
    }

    /// <summary>
    /// 
    /// </summary>
    /// <typeparam name="TEntity"></typeparam>
    /// <typeparam name="TContext"></typeparam>
    /// <typeparam name="TKey"></typeparam>
    public class CreateRepository<TEntity, TContext, TKey> : ReadRepository<TEntity, TContext, TKey>, ICreateRepository<TEntity, TKey>
           where TEntity : class, IEntity<TKey>, IEntityCreateable, new()
           where TContext : DbContext
    {
        public CreateRepository(IResolver resolver, TContext context) : base(resolver, context)
        {
        }

        public async Task<TEntity> Create(TEntity entity, CancellationToken token = default)
        {
            // Update creation date if entity is trackable
            if (entity is IEntityCreateableTrack)
                ((IEntityCreateableTrack)entity).CreatedDate = DateTime.UtcNow;

            DbContext.Add(entity);
            await Save(token);
            return entity;
        }

        public async Task<IEnumerable<TEntity>> Create(IEnumerable<TEntity> entities, CancellationToken token = default)
        {
            // update creation date 
            foreach (var e in entities)
            {
                if (e is IEntityCreateableTrack)
                   ((IEntityCreateableTrack)e).CreatedDate = DateTime.UtcNow;
            }

            // Add to context and save 
            DbContext.AddRange(entities);
            await Save(token);
            return entities;
        }
    }
}
