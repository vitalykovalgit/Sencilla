using Sencilla.Extensions.EntityFrameworkCore.Entity.Attributes;

namespace Sencilla.Repository.EntityFramework.Extension;

public class GetOrCreateQueryBuilder<TEntity>
{
    private const string COLS = nameof(COLS);
    private const string VALS = nameof(VALS);

    private readonly string[] _keys;
    private readonly QueryProvider _qp = new();

    public GetOrCreateQueryBuilder(string[] keys)
    {
        _keys = keys;
    }

    /// <summary>
    /// An insert-only MERGE that outputs the primary key of every row it inserts, then a SELECT of the primary key of
    /// every row matching the keys. Both match on one condition, so a row is «existing» exactly when the MERGE found
    /// it. Keys only, never rows: the caller reads the rows back itself, through whatever judges a read.
    /// </summary>
    public (string MergeQuery, string SelectQuery) Build(List<TEntity> entities)
    {
        var props = GetMappedProperties();
        var keyProps = ResolveKeyProperties(props);
        var targetTable = GetTargetTableName();
        var colVals = GetColumnValues(entities, props);

        var merge = $"\nMERGE {targetTable} WITH (HOLDLOCK) AS t" +
                    $"\nUSING (VALUES\n{colVals[VALS]}\n)" +
                    $"\nAS s ({colVals[COLS]})" +
                    $"\nON {BuildCondition(keyProps)}" +
                    $"\nWHEN NOT MATCHED BY TARGET THEN\n{_qp.ToInsertMergeQuery(colVals[COLS], EntityColumnMap.IsDatabaseGeneratedKey(typeof(TEntity)))}" +
                    $"\n{_qp.ToOutputMergeQuery()};";

        var keyValues = string.Join(", ", entities.Select(e =>
            "(" + string.Join(", ", keyProps.Select(p => _qp.ToSqlParameterValue(p, p.GetValue(e)))) + ")"));
        var keyCols = string.Join(", ", keyProps.Select(ColName));

        var select = $"\nSELECT t.{QueryProvider.KeyColumn} FROM {targetTable} t" +
                     $"\nINNER JOIN (VALUES {keyValues}) AS s({keyCols})" +
                     $"\nON {BuildCondition(keyProps)};";

        return (merge, select);
    }

    public List<PropertyInfo> GetMappedProperties() => EntityColumnMap.MappedProperties(typeof(TEntity));

    private List<PropertyInfo> ResolveKeyProperties(List<PropertyInfo> mappedProps)
    {
        var effectiveKeys = _keys.Length > 0 ? _keys : ["Id"];

        return effectiveKeys.Select(key =>
            mappedProps.First(p =>
                string.Equals(p.Name, key, StringComparison.OrdinalIgnoreCase) ||
                string.Equals(p.GetCustomAttribute<ColumnAttribute>()?.Name, key, StringComparison.OrdinalIgnoreCase)))
            .ToList();
    }

    private string BuildCondition(List<PropertyInfo> keyProps) =>
        string.Join(" AND ", keyProps.Select(p => $"t.{ColName(p)} = s.{ColName(p)}"));

    private Dictionary<string, string> GetColumnValues(List<TEntity> entities, List<PropertyInfo> props)
    {
        var dict = new Dictionary<string, string> { [COLS] = "", [VALS] = "" };

        dict[COLS] = string.Join(",", props.Select(ColName));

        var rows = entities.Select(e =>
            "(" + string.Join(",", props.Select(p => _qp.ToSqlParameterValue(p, p.GetValue(e)))) + ")");
        dict[VALS] = string.Join(",\n", rows);

        return dict;
    }

    private static string ColName(PropertyInfo p)
    {
        var ca = p.GetCustomAttribute<ColumnAttribute>();
        return $"[{ca?.Name ?? p.Name}]";
    }

    private static string GetTargetTableName()
    {
        var ta = typeof(TEntity).GetCustomAttribute<TableAttribute>();
        var schema = ta?.Schema == null ? "[dbo]" : $"[{ta.Schema}]";
        var table = ta?.Name == null ? $"[{typeof(TEntity).Name}]" : $"[{ta.Name}]";
        return $"{schema}.{table}";
    }
}
