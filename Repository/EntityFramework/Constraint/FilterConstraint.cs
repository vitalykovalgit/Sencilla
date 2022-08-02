
using Microsoft.EntityFrameworkCore;
using Sencilla.Core;

namespace Sencilla.Repository.EntityFramework
{
    [Implement(typeof(IReadConstraint))]
    public class FilterConstraint : IReadConstraint
    {
        public IQueryable<TEntity> Apply<TEntity>(IQueryable<TEntity> query, IFilter? filter)
        {
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

            // 
            //query = query.Where(e => EF.Property<object>(e, "Name").Equals("Milan"));

            return query;
        }
    }
}
