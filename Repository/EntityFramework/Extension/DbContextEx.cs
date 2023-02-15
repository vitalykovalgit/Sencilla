
namespace Microsoft.EntityFrameworkCore
{
    public static class DbContextEx
    {
        public static IQueryable<TEntity> Query<TEntity>(this DbContext context) where TEntity : class
        {
            return context.Set<TEntity>();
        }

        [Obsolete]
        public static async Task<IQueryable<TEntity>> Constraints<TEntity>(this IQueryable<TEntity> query, IEnumerable<IReadConstraint>? constraints, int action, IFilter? filter = null) where TEntity : class
        {
            if (constraints != null)
            {
                foreach (var c in constraints)
                    query = await c.Apply(query, action, filter);
            }
            return query;
        }
    }
}
