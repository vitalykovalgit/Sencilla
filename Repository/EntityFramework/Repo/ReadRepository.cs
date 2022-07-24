using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;
using Sencilla.Core;

namespace Sencilla.Repository.EntityFramework
{
    public class ReadRepository<TEntity, TContext, TKey> : BaseRepository<TContext>, IReadRepository<TEntity, TKey>
           where TEntity : class, IEntity<TKey>, new()
           where TContext : DbContext
    {
        public ReadRepository(IResolver resolver) : base(resolver)
        {
        }

        public async Task<IEnumerable<TEntity>> GetAll(CancellationToken token = default)
        {
            using (var context = R<TContext>())
            {
                return await context.Set<TEntity>().ToListAsync(token);
            }
        }

        public async Task<TEntity?> GetById(TKey id, params Expression<Func<TEntity, object>>[] includes)
        {
            using (var context = R<TContext>())
            {
                return await context.Set<TEntity>().FirstOrDefaultAsync(e => e.Id.Equals(id));
            }
        }

        public async Task<TEntity?> GetById(TKey id, CancellationToken token, params Expression<Func<TEntity, object>>[] includes)
        {
            using (var context = R<TContext>())
            {
                return await context.Set<TEntity>().FirstOrDefaultAsync(e => e.Id.Equals(id));
            }
        }

        public async Task<int> GetCount(CancellationToken token = default)
        {
            using (var context = R<TContext>())
            {
                return await context.Set<TEntity>().CountAsync(token);
            }
        }

        public async Task<IEnumerable<TEntity>> GetMany(IEnumerable<TKey> ids, params Expression<Func<TEntity, object>>[] includes)
        {
            using (var context = R<TContext>())
            {
                return await context.Set<TEntity>().Where(e => ids.Contains(e.Id)).ToListAsync();
            }
        }
    }
}
