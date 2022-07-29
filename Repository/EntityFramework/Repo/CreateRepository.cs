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
       where TEntity : class, IEntityCreateable<int>, new()
       where TContext : DbContext
    {
        public CreateRepository(IResolver resolver) : base(resolver) {}
    }

    /// <summary>
    /// 
    /// </summary>
    /// <typeparam name="TEntity"></typeparam>
    /// <typeparam name="TContext"></typeparam>
    /// <typeparam name="TKey"></typeparam>
    public class CreateRepository<TEntity, TContext, TKey> : ReadRepository<TEntity, TContext, TKey>, ICreateRepository<TEntity, TKey>
           where TEntity : class, IEntityCreateable<TKey>, new()
           where TContext : DbContext
    {
        public CreateRepository(IResolver resolver) : base(resolver)
        {
        }

        public async Task<TEntity> Create(TEntity entity, CancellationToken token = default)
        {
            // Update ceration date 
            entity.CreatedDate = DateTime.UtcNow;

            // Add to context and save 
            using var context = R<TContext>();
            context.Add(entity);
            await context.SaveChangesAsync(token);
            return entity;
        }

        public async Task<IEnumerable<TEntity>> Create(IEnumerable<TEntity> entities, CancellationToken token = default)
        {
            // update creation date 
            foreach (var e in entities)
                e.CreatedDate = DateTime.UtcNow;

            // Add to context and save 
            using var context = R<TContext>();
            context.AddRange(entities, token);
            await context.SaveChangesAsync(token);
            return entities;
        }
    }
}
