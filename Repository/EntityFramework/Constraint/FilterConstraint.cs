#pragma warning disable CS1998 // Async method lacks 'await' operators and will run synchronously

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
                    var expr = kvp.Value.ToExpression(); // property values 
                    query = query.Where(expr);
                }
            }

            return query;
        }
    }
}

#pragma warning restore CS1998 // Async method lacks 'await' operators and will run synchronously
