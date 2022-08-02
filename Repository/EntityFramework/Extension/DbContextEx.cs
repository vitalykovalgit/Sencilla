
using Sencilla.Core;

namespace Microsoft.EntityFrameworkCore
{
    public static class DbContextEx
    {
        public static IQueryable<TEntity> Query<TEntity>(this DbContext context) where TEntity : class
        {
            return context.Set<TEntity>() as IQueryable<TEntity>;
        }

        public static IQueryable<TEntity> Constraints<TEntity>(this IQueryable<TEntity> query, IEnumerable<IReadConstraint>? constraints, IFilter? filter = null) where TEntity : class
        {
            if (constraints != null)
            {
                foreach (var c in constraints)
                    query = c.Apply(query, filter);
            }
            return query;
        }
    }
}
