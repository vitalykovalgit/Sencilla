using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;
using Sencilla.Core;

namespace Sencilla.Repository.EntityFramework
{
    /// <summary>
    /// 
    /// </summary>
    /// <typeparam name="TEntity"></typeparam>
    /// <typeparam name="TContext"></typeparam>
    public class ReadRepository<TEntity, TContext> : ReadRepository<TEntity, TContext, int>, IReadRepository<TEntity>
       where TEntity : class, IEntity<int>, new()
       where TContext : DbContext
    {
        public ReadRepository(IResolver resolver) : base(resolver) {}
    }

    /// <summary>
    /// 
    /// </summary>
    /// <typeparam name="TEntity"></typeparam>
    /// <typeparam name="TContext"></typeparam>
    /// <typeparam name="TKey"></typeparam>
    public class ReadRepository<TEntity, TContext, TKey> : BaseRepository<TContext>, IReadRepository<TEntity, TKey>
           where TEntity : class, IEntity<TKey>, new()
           where TContext : DbContext
    {
        public ReadRepository(IResolver resolver) : base(resolver)
        {
        }

        public async Task<TEntity?> GetById(TKey id, CancellationToken token = default, params Expression<Func<TEntity, object>>[] with)
        {
            using (var ctx = R<TContext>())
            {
                return await ctx.Query<TEntity>()
                                .Constraints(Constraints)
                                .FirstOrDefaultAsync(e => e.Id.Equals(id), token);
            }
        }

        public async Task<IEnumerable<TEntity>> GetByIds(IEnumerable<TKey> ids, CancellationToken token = default, params Expression<Func<TEntity, object>>[] includes)
        {
            using (var ctx = R<TContext>())
            {
                return await ctx.Query<TEntity>()
                                .Constraints(Constraints)
                                .Where(e => ids.Contains(e.Id))
                                .ToListAsync(token);
            }
        }

        public async Task<IEnumerable<TEntity>> GetAll(IFilter? filter = null, CancellationToken token = default, params Expression<Func<TEntity, object>>[] with)
        {
            using (var ctx = R<TContext>())
            {
                return await ctx.Query<TEntity>()
                                .Constraints(Constraints, filter)
                                .ToListAsync(token);
            }
        }

        public async Task<int> GetCount(IFilter? filter = null, CancellationToken token = default)
        {
            using (var ctx = R<TContext>())
            {
                return await ctx.Query<TEntity>()
                                .Constraints(Constraints, filter)
                                .CountAsync(token);
            }
        }
    }
}
