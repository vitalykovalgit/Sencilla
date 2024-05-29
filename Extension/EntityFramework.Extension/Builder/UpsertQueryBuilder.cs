namespace Sencilla.Repository.EntityFramework.Extension;

public class UpsertQueryBuilder<TEntity>
{
    private const string COLS = nameof(COLS);
    private const string VALS = nameof(VALS);
    private readonly UpsertCommand<TEntity> _cmnd;
    private readonly QueryProvider _qp;

    public UpsertQueryBuilder(UpsertCommand<TEntity> cmnd)
    {
        _cmnd = cmnd;
        _qp = new QueryProvider();
    }

    public string Build(TEntity e)
    {
        string query = string.Empty;
        var targetTable = GetTargetTableName(e);

        /*
        if (_cmnd.InsertAction != null)
        {
            query += $"SET IDENTITY_INSERT {targetTable} ON";
        }*/

        var colVals = GetColumnValues(e);
        query += Environment.NewLine + $"MERGE {targetTable} AS t";
        query += Environment.NewLine + "USING (VALUES";
        query += Environment.NewLine + $"({colVals[VALS]}))";
        query += Environment.NewLine + $"AS s ({colVals[COLS]})";

        query += Environment.NewLine + "ON " + $"{_qp.ToMergeQuery(_cmnd.MatchedCondition, QueryClauseType.MergeMatchCondition)}";

        if (_cmnd.InsertAction != null)
        {
            query += Environment.NewLine + "WHEN NOT MATCHED BY TARGET THEN" + Environment.NewLine
                + _qp.ToMergeQuery(_cmnd.InsertAction, QueryClauseType.MergeInsertCondition);
        }
        else
        {
            query += Environment.NewLine + "WHEN NOT MATCHED BY TARGET THEN" + Environment.NewLine
                + _qp.ToInsertMergeQuery(colVals[COLS]);
        }

        if (_cmnd.UpdateAction != null)
        {
            query += Environment.NewLine + "WHEN MATCHED THEN UPDATE" + Environment.NewLine
                + _qp.ToMergeQuery(_cmnd.UpdateAction, QueryClauseType.MergeUpdateCondition);
        }
        else
        {
            query += Environment.NewLine + "WHEN MATCHED THEN UPDATE" + Environment.NewLine
                + _qp.ToUpdateMergeQuery(colVals[COLS]);
        }

        query += Environment.NewLine + ";";

        /*
        if (_cmnd.InsertAction != null)
        {
            query += $"SET IDENTITY_INSERT {targetTable} OFF";
        }*/

        return query;
    }

    private string GetTargetTableName(TEntity e)
    {
        var ta = e.GetType().GetCustomAttribute<TableAttribute>();

        var schema = ta?.Schema == null ? "[dbo]" : $"[{ta?.Schema}]";
        var table = ta?.Name == null ? $"[{e.GetType().Name}]" : $"[{ta?.Name}]";

        return string.Join(".", schema, table);
    }

    private Dictionary<string, string> GetColumnValues(TEntity e)
    {
        var dict = new Dictionary<string, string>()
        {
            { COLS, string.Empty },
            { VALS, string.Empty },
        };

        var props = e.GetType().GetProperties();

        foreach (var p in props)
        {
            var nma = p.GetCustomAttribute<NotMappedAttribute>();
            if (nma == null)
            {
                var ca = p.GetCustomAttribute<ColumnAttribute>();
                dict[COLS] += $"[{ca?.Name ?? p.Name}],";

                var ov = _qp.ToSqlParameterValue(p, p.GetValue(e));
                dict[VALS] += $"{ov},";
            }
        }

        dict[COLS] = dict[COLS].TrimEnd(',');
        dict[VALS] = dict[VALS].TrimEnd(',');

        return dict;
    }
}
