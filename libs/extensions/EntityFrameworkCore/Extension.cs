using System.Data;
using System.Data.Common;
using Sencilla.Core;

namespace Microsoft.EntityFrameworkCore;

public static class EntityFrameworkCoreExtensions
{
    public static Task<(IEnumerable<TEntity> Created, IEnumerable<TEntity> Existing)> GetOrCreateBulkAsync<TEntity, TContext>(
        this TContext context,
        IEnumerable<TEntity> entities,
        string[] keys,
        CancellationToken token = default)
        where TEntity : class, IBaseEntity, new()
        where TContext : DbContext
        => context.GetOrCreateBulkAsync<TEntity, TContext>(entities, keys, filter: null, token);

    public static async Task<(IEnumerable<TEntity> Created, IEnumerable<TEntity> Existing)> GetOrCreateBulkAsync<TEntity, TContext>(
        this TContext context,
        IEnumerable<TEntity> entities,
        string[] keys,
        Filter? filter,
        CancellationToken token = default)
        where TEntity : class, IBaseEntity, new()
        where TContext : DbContext
    {
        var entityList = entities.ToList();
        if (entityList.Count == 0)
            return ([], []);

        // Fast path: no Include requested — keep the single-batch full-row reflection map.
        if (filter?.With is null || filter.With.Length == 0)
            return await GetOrCreateBulkFullRowAsync<TEntity, TContext>(context, entityList, keys, token);

        // Include path: MERGE+SELECT return only PK ids; EF reloads with .Include() + AsNoTracking.
        return await GetOrCreateBulkWithIncludesAsync(context, entityList, keys, filter.With, token);
    }

    private static async Task<(IEnumerable<TEntity> Created, IEnumerable<TEntity> Existing)> GetOrCreateBulkFullRowAsync<TEntity, TContext>(
        TContext context,
        List<TEntity> entityList,
        string[] keys,
        CancellationToken token)
        where TEntity : class, new()
        where TContext : DbContext
    {
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

    private static async Task<(IEnumerable<TEntity> Created, IEnumerable<TEntity> Existing)> GetOrCreateBulkWithIncludesAsync<TEntity, TContext>(
        TContext context,
        List<TEntity> entityList,
        string[] keys,
        string[] with,
        CancellationToken token)
        where TEntity : class, IBaseEntity, new()
        where TContext : DbContext
    {
        var builder = new GetOrCreateQueryBuilder<TEntity>(keys);
        var props = builder.GetMappedProperties();
        var pkProp = GetOrCreateQueryBuilder<TEntity>.GetPrimaryKeyProperty(props);
        var (mergeQuery, selectQuery) = builder.BuildIdsOnly(entityList);

        var insertedIds = new HashSet<object>();
        var allIds = new List<object>();

        var connection = context.Database.GetDbConnection();
        var wasOpen = connection.State == ConnectionState.Open;
        if (!wasOpen)
            await connection.OpenAsync(token);

        try
        {
            using var cmd = connection.CreateCommand();
            cmd.CommandText = mergeQuery + "\n" + selectQuery;

            using var reader = await cmd.ExecuteReaderAsync(token);

            // Result set 1: ids of newly inserted rows
            while (await reader.ReadAsync(token))
                insertedIds.Add(reader.GetValue(0));

            // Result set 2: ids of ALL matched rows (existing + newly inserted)
            await reader.NextResultAsync(token);
            while (await reader.ReadAsync(token))
                allIds.Add(reader.GetValue(0));
        }
        finally
        {
            if (!wasOpen)
                await connection.CloseAsync();
        }

        if (allIds.Count == 0)
            return ([], []);

        // Reload via EF so navigation properties materialize through .Include().
        var query = BuildIdInQuery<TEntity>(context, pkProp, allIds);

        foreach (var path in with)
        {
            var canonical = ResolveNavigationPath<TEntity>(path);
            query = query.Include(canonical);
        }

        var loaded = await query.AsNoTracking().ToListAsync(token);

        var created = new List<TEntity>();
        var existing = new List<TEntity>();
        foreach (var e in loaded)
        {
            var id = pkProp.GetValue(e)!;
            if (insertedIds.Contains(id))
                created.Add(e);
            else
                existing.Add(e);
        }

        return (created, existing);
    }

    private static IQueryable<TEntity> BuildIdInQuery<TEntity>(DbContext context, PropertyInfo idProp, List<object> ids)
        where TEntity : class
    {
        var idType = idProp.PropertyType;
        var typedArray = Array.CreateInstance(idType, ids.Count);
        for (var i = 0; i < ids.Count; i++)
            typedArray.SetValue(Convert.ChangeType(ids[i], idType), i);

        // Build: e => typedArray.Contains(e.Id) — translates to SQL IN (...)
        var param = Expression.Parameter(typeof(TEntity), "e");
        var idAccess = Expression.Property(param, idProp);

        var containsMethod = typeof(Enumerable).GetMethods()
            .First(m => m.Name == nameof(Enumerable.Contains) && m.GetParameters().Length == 2)
            .MakeGenericMethod(idType);

        var containsCall = Expression.Call(null, containsMethod, Expression.Constant(typedArray), idAccess);
        var lambda = Expression.Lambda<Func<TEntity, bool>>(containsCall, param);

        return context.Set<TEntity>().Where(lambda);
    }

    /// <summary>
    /// Walks a dotted include path (e.g. "items.products") case-insensitively against the
    /// entity model and returns the canonical PascalCase path EF requires. Throws when a
    /// segment doesn't exist so the API surfaces a clear error instead of EF's cryptic one.
    /// </summary>
    private static string ResolveNavigationPath<TEntity>(string path)
    {
        var segments = path.Split('.');
        var current = typeof(TEntity);
        var resolved = new string[segments.Length];

        for (var i = 0; i < segments.Length; i++)
        {
            var prop = current.GetProperties()
                .FirstOrDefault(p => string.Equals(p.Name, segments[i], StringComparison.OrdinalIgnoreCase))
                ?? throw new InvalidOperationException(
                    $"Navigation property '{segments[i]}' not found on {current.Name} (path: '{path}').");

            resolved[i] = prop.Name;

            // Walk into the next type — element type for collections, declared type otherwise.
            var propType = prop.PropertyType;
            if (propType.IsGenericType)
            {
                var elementType = propType.GetGenericArguments().FirstOrDefault();
                current = elementType is not null && elementType.IsAssignableTo(typeof(IBaseEntity))
                    ? elementType
                    : propType;
            }
            else
            {
                current = propType;
            }
        }

        return string.Join(".", resolved);
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
        // Nothing to merge — bail out. An empty source would otherwise emit a phantom
        // VALUES row (typed [Id] = 0, which clashes with a uniqueidentifier key) AND an
        // unscoped "WHEN NOT MATCHED BY SOURCE THEN DELETE" that would wipe the whole table.
        if (entities is null || !entities.Any())
            return;

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
