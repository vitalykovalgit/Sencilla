
namespace Microsoft.EntityFrameworkCore;

public static class DbContextEx
{
    public static IQueryable<TEntity> Query<TEntity>(this DbContext context) where TEntity : class
    {
        return context.Set<TEntity>();
    }

    /// <summary>
    /// Atomically sets <paramref name="key"/> -> <paramref name="value"/> on the
    /// JSON dictionary property selected by <paramref name="property"/> for the
    /// entity identified by <paramref name="id"/>. Uses SQL Server JSON_MODIFY,
    /// avoiding read-modify-write so concurrent writes to other keys are preserved.
    /// </summary>
    public static Task<int> JsonMergeAsync<TEntity, TKey, TValue>(this DbContext context, TKey id,
        Expression<Func<TEntity, IDictionary<string, TValue>?>> property, string key, TValue value, CancellationToken token = default) where TEntity : class
    {
        var memberName = ((property.Body as MemberExpression
            ?? (property.Body as UnaryExpression)?.Operand as MemberExpression)?.Member.Name)
            ?? throw new ArgumentException("Property selector must be a member expression.", nameof(property));

        var entityType = context.Model.FindEntityType(typeof(TEntity))
            ?? throw new InvalidOperationException($"Entity '{typeof(TEntity).Name}' is not mapped.");
        var column = entityType.FindProperty(memberName)?.GetColumnName()
            ?? throw new InvalidOperationException($"Property '{memberName}' is not mapped.");
        var idColumn = entityType.FindPrimaryKey()!.Properties[0].GetColumnName();
        var table = entityType.GetTableName()!;
        var schema = entityType.GetSchema() ?? "dbo";

        // JSON_MODIFY accepts a path variable, but we keep the path inline so
        // identifiers stay where SQL parameters can't go. Only data is parameterized.
        var path = BuildJsonPath(key);
        var json = JsonSerializer.Serialize(value, JsonMergeOptions);

        // Doubled braces escape the format-string placeholders used by ExecuteSqlRawAsync.
        var sql =
            $"UPDATE [{schema}].[{table}] " +
            $"SET [{column}] = JSON_MODIFY(ISNULL([{column}], N'{{{{}}}}'), N'{path}', JSON_QUERY({{0}})) " +
            $"WHERE [{idColumn}] = {{1}}";

        return context.Database.ExecuteSqlRawAsync(sql, [json, id!], token);
    }

    private static readonly JsonSerializerOptions JsonMergeOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        NumberHandling = JsonNumberHandling.AllowReadingFromString
    };

    private static string BuildJsonPath(string key)
    {
        var escaped = key.Replace("\\", "\\\\").Replace("\"", "\\\"").Replace("'", "''");
        return $"$.\"{escaped}\"";
    }
}
