using System.Data;
using System.Data.Common;

namespace Microsoft.EntityFrameworkCore;

public static class EntityFrameworkCoreExtensions
{
    public static async Task<(IEnumerable<TEntity> Created, IEnumerable<TEntity> Existing)> GetOrCreateBulkAsync<TEntity, TContext>(
        this TContext context,
        IEnumerable<TEntity> entities,
        string[] keys,
        CancellationToken token = default)
        where TEntity : class, new()
        where TContext : DbContext
    {
        var entityList = entities.ToList();
        if (entityList.Count == 0)
            return ([], []);

        var builder = new GetOrCreateQueryBuilder<TEntity>(keys);
        var props = builder.GetMappedProperties();
        var keyProps = builder.ResolveKeyProperties(props);
        var (mergeQuery, selectQuery) = builder.Build(entityList);

        var connection = context.Database.GetDbConnection();
        var wasOpen = connection.State == ConnectionState.Open;
        if (!wasOpen)
            await connection.OpenAsync(token);

        try
        {
            using var cmd = connection.CreateCommand();
            cmd.CommandText = mergeQuery + "\n" + selectQuery;

            using var reader = await cmd.ExecuteReaderAsync(token);

            // Result set 1: key values of rows just inserted (OUTPUT INSERTED.*)
            var insertedKeys = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            while (await reader.ReadAsync(token))
            {
                var composite = BuildKeyComposite(reader, keyProps);
                insertedKeys.Add(composite);
            }

            // Result set 2: all matching rows (existing + newly inserted)
            await reader.NextResultAsync(token);

            var created = new List<TEntity>();
            var existing = new List<TEntity>();

            while (await reader.ReadAsync(token))
            {
                var entity = MapRow<TEntity>(reader, props);
                var composite = BuildKeyComposite(reader, keyProps);

                if (insertedKeys.Contains(composite))
                    created.Add(entity);
                else
                    existing.Add(entity);
            }

            return (created, existing);
        }
        finally
        {
            if (!wasOpen)
                await connection.CloseAsync();
        }
    }

    private static string BuildKeyComposite(DbDataReader reader, List<System.Reflection.PropertyInfo> keyProps)
    {
        var parts = keyProps.Select(p =>
        {
            var colName = p.GetCustomAttribute<System.ComponentModel.DataAnnotations.Schema.ColumnAttribute>()?.Name ?? p.Name;
            try { return reader[colName]?.ToString() ?? ""; }
            catch { return ""; }
        });
        return string.Join("|", parts);
    }

    private static TEntity MapRow<TEntity>(DbDataReader reader, List<System.Reflection.PropertyInfo> props)
        where TEntity : new()
    {
        var entity = new TEntity();
        foreach (var prop in props)
        {
            var colName = prop.GetCustomAttribute<System.ComponentModel.DataAnnotations.Schema.ColumnAttribute>()?.Name ?? prop.Name;
            try
            {
                var ordinal = reader.GetOrdinal(colName);
                if (reader.IsDBNull(ordinal))
                    continue;

                var value = reader.GetValue(ordinal);
                var targetType = Nullable.GetUnderlyingType(prop.PropertyType) ?? prop.PropertyType;
                prop.SetValue(entity, Convert.ChangeType(value, targetType));
            }
            catch { /* column not found or type mismatch — skip */ }
        }
        return entity;
    }


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
