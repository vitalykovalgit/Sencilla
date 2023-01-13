#pragma warning disable CS1998 // Async method lacks 'await' operators and will run synchronously

using Microsoft.EntityFrameworkCore;
using Sencilla.Core;

namespace Sencilla.Repository.EntityFramework
{
    [Implement(typeof(IReadConstraint))]
    public class FilterConstraint : IReadConstraint
    {
        public async Task<IQueryable<TEntity>> Apply<TEntity>(IQueryable<TEntity> query, IFilter? filter)
        {
            if (filter?.Skip != null)
            {
                query = query.Skip(filter.Skip.Value);
            }

            if (filter?.Take != null)
            {
                query = query.Take(filter.Take.Value);
            }

            if (filter?.OrderBy?.Length > 0)
            {
                query = (filter.Descending ?? false)
                      ? query.OrderByDescending(e => EF.Property<object>(e, filter.OrderBy.First()))
                      : query.OrderBy(e => EF.Property<object>(e, filter.OrderBy.First()));
            }

            if (filter?.Properties?.Count > 0)
            {
                foreach (var kvp in filter.Properties)
                {
                    var prop = kvp.Key; // property name 
                    var values = kvp.Value; // property values 

                    query = query.Where(e => values.Contains(EF.Property<object>(e, prop)));
                }
            }

            return query;
        }
    }
}

#pragma warning restore CS1998 // Async method lacks 'await' operators and will run synchronously
