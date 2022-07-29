using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;
using Sencilla.Core;

namespace Sencilla.Repository.EntityFramework
{
    public class ReadRepository<TEntity, TContext> : ReadRepository<TEntity, TContext, int>, IReadRepository<TEntity>
       where TEntity : class, IEntity<int>, new()
       where TContext : DbContext
    {
        public ReadRepository(IResolver resolver) : base(resolver) {}
    }

    public class ReadRepository<TEntity, TContext, TKey> : BaseRepository<TContext>, IReadRepository<TEntity, TKey>
           where TEntity : class, IEntity<TKey>, new()
           where TContext : DbContext
    {
        public ReadRepository(IResolver resolver) : base(resolver)
        {
        }

        public async Task<TEntity?> GetById(TKey id, CancellationToken token = default, params Expression<Func<TEntity, object>>[] with)
        {
            using (var context = R<TContext>())
            {
                return await context.Set<TEntity>().FirstOrDefaultAsync(e => e.Id.Equals(id), token);
            }
        }

        public async Task<IEnumerable<TEntity>> GetByIds(IEnumerable<TKey> ids, CancellationToken token = default, params Expression<Func<TEntity, object>>[] includes)
        {
            using (var context = R<TContext>())
            {
                return await context.Set<TEntity>().Where(e => ids.Contains(e.Id)).ToListAsync(token);
            }
        }

        public async Task<IEnumerable<TEntity>> GetAll(IFilter? filter = null, CancellationToken token = default, params Expression<Func<TEntity, object>>[] with)
        {
            using (var context = R<TContext>())
            {
                var query = context.Set<TEntity>() as IQueryable<TEntity>;

                if (filter?.Skip != null)
                    query = query.Skip(filter.Skip.Value);

                if (filter?.Take != null)
                    query = query.Take(filter.Take.Value);

                if (filter?.OrderBy?.Length > 0)
                {
                    query = (filter.Descending ?? false)
                          ? query.OrderByDescending(e => EF.Property<object>(e, filter.OrderBy.First()))
                          : query.OrderBy(e => EF.Property<object>(e, filter.OrderBy.First()));
                }

                //query = query.Where(e => EF.Property<object>(e, "Name").Equals("Milan"));

                return await query.ToListAsync(token);
            }
        }


        public async Task<int> GetCount(IFilter? filter = null, CancellationToken token = default)
        {
            using (var context = R<TContext>())
            {
                return await context.Set<TEntity>().CountAsync(token);
            }
        }
    }
}
