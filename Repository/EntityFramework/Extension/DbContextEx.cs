
namespace Microsoft.EntityFrameworkCore;

public static class DbContextEx
{
    public static IQueryable<TEntity> Query<TEntity>(this DbContext context) where TEntity : class
    {
        return context.Set<TEntity>();
    }
}
