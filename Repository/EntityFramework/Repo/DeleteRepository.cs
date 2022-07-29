using Microsoft.EntityFrameworkCore;
using Z.EntityFramework.Plus;
using Sencilla.Core;

namespace Sencilla.Repository.EntityFramework
{
    public class DeleteRepository<TEntity, TContext> : DeleteRepository<TEntity, TContext, int>, IDeleteRepository<TEntity>
       where TEntity : class, IEntityDeleteable<int>, new()
       where TContext : DbContext
    {
        public DeleteRepository(IResolver resolver) : base(resolver) {}
    }

    /// <summary>
    /// 
    /// </summary>
    /// <typeparam name="TEntity"></typeparam>
    /// <typeparam name="TContext"></typeparam>
    /// <typeparam name="TKey"></typeparam>
    public class DeleteRepository<TEntity, TContext, TKey> : ReadRepository<TEntity, TContext, TKey>, IDeleteRepository<TEntity, TKey>
           where TEntity : class, IEntityDeleteable<TKey>, new()
           where TContext : DbContext
    {
        public DeleteRepository(IResolver resolver) : base(resolver)
        {
        }

        public async Task<int> Delete(TKey id, CancellationToken token = default)
        {
            using var context = R<TContext>();
            var entity = context.Set<TEntity>().FirstOrDefault(e => e.Id.Equals(id));
            if (entity != null)
            {
                context.Remove(entity);
                return await context.SaveChangesAsync(token);
            }

            return 0;
        }

        public async Task<int> Delete(TEntity entity, CancellationToken token = default)
        {
            using var context = R<TContext>();
            context.Remove(entity);
            var count = await context.SaveChangesAsync(token);
            return count;
        }

        public async Task<int> Delete(IEnumerable<TKey> ids, CancellationToken token = default)
        {
            using var context = R<TContext>();
            var count = await context.Set<TEntity>().Where(e => ids.Contains(e.Id)).DeleteAsync(token);
            await context.SaveChangesAsync(token);
            return count;
        }

        public async Task<int> Delete(IEnumerable<TEntity> entities, CancellationToken token = default)
        {
            using var context = R<TContext>();
            context.RemoveRange(entities);
            var count = await context.SaveChangesAsync(token);
            return count;
        }
    }
}
