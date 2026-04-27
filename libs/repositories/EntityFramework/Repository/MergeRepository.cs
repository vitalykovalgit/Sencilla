namespace Sencilla.Repository.EntityFramework;

public class MergeRepository<TEntity, TContext>(RepositoryDependency dependency, TContext context)
    : MergeRepository<TEntity, TContext, int>(dependency, context), IMergeRepository<TEntity>
    where TEntity : class, IEntity<int>, IEntityMergeable, new()
    where TContext : DbContext;

public class MergeRepository<TEntity, TContext, TKey>(RepositoryDependency dependency, TContext context)
    : ReadRepository<TEntity, TContext, TKey>(dependency, context), IMergeRepository<TEntity, TKey>
    where TEntity : class, IEntity<TKey>, IEntityMergeable, new()
    where TContext : DbContext
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        NumberHandling = JsonNumberHandling.AllowReadingFromString
    };

    public Task<int> MergeAsync<TValue>(TKey id,  Expression<Func<TEntity, IDictionary<string, TValue>?>> property,
        string key, TValue value, CancellationToken token = default)
    {
        var memberName = (property.Body as MemberExpression
            ?? (property.Body as UnaryExpression)?.Operand as MemberExpression)?.Member.Name
            ?? throw new ArgumentException("Property selector must be a member expression.", nameof(property));

        var entityType = DbContext.Model.FindEntityType(typeof(TEntity))
            ?? throw new InvalidOperationException($"Entity '{typeof(TEntity).Name}' is not mapped.");
        var column = entityType.FindProperty(memberName)?.GetColumnName()
            ?? throw new InvalidOperationException($"Property '{memberName}' is not mapped.");
        var idColumn = entityType.FindPrimaryKey()!.Properties[0].GetColumnName();
        var table = entityType.GetTableName()!;
        var schema = entityType.GetSchema() ?? "dbo";

        // JSON_MODIFY requires a literal path; parameterize only the value and id.
        // Doubled braces escape the format-string placeholders used by ExecuteSqlRawAsync.
        var path = $"$.\"{key.Replace("\\", "\\\\").Replace("\"", "\\\"").Replace("'", "''")}\"";
        var json = JsonSerializer.Serialize(value, JsonOptions);

        var sql =
            $"UPDATE [{schema}].[{table}] " +
            $"SET [{column}] = JSON_MODIFY(ISNULL([{column}], N'{{{{}}}}'), N'{path}', JSON_QUERY({{0}})) " +
            $"WHERE [{idColumn}] = {{1}}";

        return DbContext.Database.ExecuteSqlRawAsync(sql, [json, id!], token);
    }
}
