namespace Sencilla.Repository.EntityFramework.Extension;

public static class EfExtension
{
    public static async Task UpsertAsync<TEntity, TContext>(this TContext context,
        TEntity e,
        Expression<Func<TEntity, object>> condition,
        Expression<Func<TEntity, TEntity>>? insertAction = null,
        Expression<Func<TEntity, TEntity>>? updateAction = null)
        where TEntity : class
        where TContext : DbContext
    {
        var cmnd = new UpsertCommand<TEntity>(condition)
        {
            InsertAction = insertAction,
            UpdateAction = updateAction,
        };

        var builder = new UpsertQueryBuilder<TEntity>(cmnd);

        var query = builder.Build(e);

        await context.Database.ExecuteSqlRawAsync(query);
    }

    // todo: implement bulk to execute by one query
    public static async Task UpsertBulkAsync<TEntity>(this DbContext context,
        IEnumerable<TEntity> entities,
        Expression<Func<TEntity, object>> condition,
        Expression<Func<TEntity, TEntity>>? insertAction = null,
        Expression<Func<TEntity, TEntity>>? updateAction = null) where TEntity : class
    {
        foreach (var e in entities)
        {
            await context.UpsertAsync(e, condition, insertAction, updateAction);
        }
    }
}
