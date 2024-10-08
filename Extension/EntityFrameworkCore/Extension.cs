﻿namespace Microsoft.EntityFrameworkCore;

public static class EntityFrameworkCoreExtensions
{
    public static Task UpsertAsync<TEntity, TContext>(this TContext context,
        TEntity e,
        Expression<Func<TEntity, object>> condition,
        Expression<Func<TEntity, TEntity>>? insertAction = null,
        Expression<Func<TEntity, TEntity>>? updateAction = null)
        where TEntity : class
        where TContext : DbContext 
    {
        return UpsertBulkAsync(context, new List<TEntity> { e }, condition, insertAction, updateAction);
    }


    public static async Task UpsertBulkAsync<TEntity, TContext>(this TContext context,
        IEnumerable<TEntity> entities,
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

        var query = builder.Build(entities);

        await context.Database.ExecuteSqlRawAsync(query);
    }

    public static Task MergeAsync<TEntity, TContext>(this TContext context,
        TEntity e,
        Expression<Func<TEntity, object>> condition,
        Expression<Func<TEntity, TEntity>>? insertAction = null,
        Expression<Func<TEntity, TEntity>>? updateAction = null)
        where TEntity : class
        where TContext : DbContext
    {
        return MergeBulkAsync(context, new List<TEntity> { e }, condition, insertAction, updateAction);
    }

    public static async Task MergeBulkAsync<TEntity, TContext>(this TContext context,
        IEnumerable<TEntity> entities,
        Expression<Func<TEntity, object>> condition,
        Expression<Func<TEntity, TEntity>>? insertAction = null,
        Expression<Func<TEntity, TEntity>>? updateAction = null)
        where TEntity : class
        where TContext : DbContext
    {
        var cmnd = new MergeCommand<TEntity>(condition)
        {
            InsertAction = insertAction,
            UpdateAction = updateAction
        };

        var builder = new MergeQueryBuilder<TEntity>(cmnd);

        var query = builder.Build(entities);

        await context.Database.ExecuteSqlRawAsync(query);
    }
}
