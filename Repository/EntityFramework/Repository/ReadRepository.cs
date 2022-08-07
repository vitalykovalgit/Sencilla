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
        public ReadRepository(IResolver resolver, TContext context) : base(resolver, context) { }
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
        public ReadRepository(IResolver resolver, TContext context) : base(resolver, context)
        {
        }

#pragma warning disable CS8602 // Dereference of a possibly null reference.
        public async Task<TEntity?> GetById(TKey id, CancellationToken token = default, params Expression<Func<TEntity, object>>[] with)
        {
            var query = await Query(null);
            return await query.FirstOrDefaultAsync(e => e.Id.Equals(id), token);
        }
#pragma warning restore CS8602 // Dereference of a possibly null reference.

        public async Task<IEnumerable<TEntity>> GetByIds(IEnumerable<TKey> ids, CancellationToken token = default, params Expression<Func<TEntity, object>>[] includes)
        {
            var query = await Query(null);
            return await query.Where(e => ids.Contains(e.Id)).ToListAsync(token);
        }

        public async Task<IEnumerable<TEntity>> GetAll(IFilter? filter = null, CancellationToken token = default, params Expression<Func<TEntity, object>>[] with)
        {
            var query = await Query(filter);
            return await query.ToListAsync(token);
        }

        public async Task<int> GetCount(IFilter? filter = null, CancellationToken token = default)
        {
            var query = await Query(filter);
            return await query.CountAsync(token);
        }

        protected Task<IQueryable<TEntity>> Query(IFilter? filter)
        {
            return DbContext.Query<TEntity>()
                            .AsNoTracking()
                            .Constraints(Constraints, filter);
        }
    }
}
