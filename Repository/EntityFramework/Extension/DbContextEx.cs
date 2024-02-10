namespace Microsoft.EntityFrameworkCore;

public static class DbContextEx
{
    public static IQueryable<TEntity> Query<TEntity>(this DbContext context) where TEntity : class
    {
        return context.Set<TEntity>();
    }

    public static IEnumerable<TEntity> BulkMerge<TEntity>(this DbContext context, IEnumerable<TEntity> entities,
        Action<BulkMergeOptions<TEntity>> configureOptions = null) where TEntity : class
    {
        var options = new BulkMergeOptions<TEntity>();
        var results = new List<TEntity>(entities.Count());

        configureOptions?.Invoke(options);

        var dbSet = context.Set<TEntity>();

        foreach (var entity in entities)
        {
            var primaryKeyValue = options.PKColumn.GetValue(entity);
            var existingEntity = dbSet.Find(primaryKeyValue);

            if (existingEntity != null)
            {
                context.Entry(existingEntity).CurrentValues.SetValues(entity);
                results.Add(entity);
            }
            else
            {
                var created = dbSet.Add(entity);
                results.Add(created.Entity);
            }
        }

        context.SaveChanges();

        return results;
    }
}
