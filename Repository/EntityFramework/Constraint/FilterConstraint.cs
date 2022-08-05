
using Microsoft.EntityFrameworkCore;
using Sencilla.Core;

namespace Sencilla.Repository.EntityFramework
{
    [Implement(typeof(IReadConstraint))]
    public class FilterConstraint : IReadConstraint
    {
#pragma warning disable CS1998 // Async method lacks 'await' operators and will run synchronously
        public async Task<IQueryable<TEntity>> Apply<TEntity>(IQueryable<TEntity> query, IFilter? filter)
#pragma warning restore CS1998 // Async method lacks 'await' operators and will run synchronously
        {
            if (filter?.Skip != null)
                query = query.Skip(filter.Skip.Value);

            if (filter?.Take != null)
                query = query.Take(filter.Take.Value);

            if (filter?.OrderBy?.Length > 0)
            {
#pragma warning disable CS8604 // Possible null reference argument.
                query = (filter.Descending ?? false)
                      ? query.OrderByDescending(e => EF.Property<object>(e, filter.OrderBy.First()))
                      : query.OrderBy(e => EF.Property<object>(e, filter.OrderBy.First()));
#pragma warning restore CS8604 // Possible null reference argument.
            }

            // 
            //query = query.Where(e => EF.Property<object>(e, "Name").Equals("Milan"));

            return query;
        }
    }
}
