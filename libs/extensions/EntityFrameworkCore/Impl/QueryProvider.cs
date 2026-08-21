namespace Sencilla.Repository.EntityFramework.Extension;

public class QueryProvider
{
    public string ToMergeQuery(Expression e, QueryClauseType t)
    {
        var tr = new TranslatorFactory().Get(t);
        tr.Translate(e);
        return tr.Condition;
    }

    public string ToSqlParameterValue(PropertyInfo p, object ov)
    {
        if (ov == null)
            return $"NULL";

        if (p.PropertyType == typeof(string))
            return $"N'{Sanitize(ov.ToString())}'";


        if (p.PropertyType == typeof(DateTime))
            return $"'{((DateTime)ov).ToString("yyyy-MM-dd HH:mm:ss")}'";

        if (p.PropertyType == typeof(Guid)
            || p.PropertyType == typeof(TimeSpan)
            || p.PropertyType == typeof(DateTimeOffset))
            return $"'{ov}'";

        if (p.PropertyType == typeof(bool))
            return bool.Parse(ov.ToString()) ? "1" : "0";


        return ov.ToString();
    }

    /// <summary>
    /// Builds the MERGE's `WHEN NOT MATCHED THEN INSERT` clause. <paramref name="keyIsDatabaseGenerated"/>
    /// decides whether [Id] is named: an IDENTITY column must be left out, a client-supplied key
    /// (string, Guid) must be included or the insert writes NULL into a NOT NULL column.
    /// See <see cref="EntityColumnMap.IsDatabaseGeneratedKey"/>.
    /// </summary>
    public string ToInsertMergeQuery(string eCols, bool keyIsDatabaseGenerated = true)
    {
        var insertCols = keyIsDatabaseGenerated ? ExcludeIdColumn(eCols) : eCols;

        var insertVals = string.Empty;
        var cols = insertCols.Split(",");

        foreach (var c in cols)
        {
            insertVals += $"s.{c},";
        }

        return "INSERT (" + insertCols + ")" + Environment.NewLine + "VALUES (" + insertVals.TrimEnd(',') + ")";
    }

    public string ToUpdateMergeQuery(string eCols)
    {
        var res = string.Empty;
        var cols = ExcludeIdColumn(eCols).Split(",");

        foreach (var c in cols)
        {
            res += $"t.{c} = s.{c}" + Environment.NewLine + ",";
        }

        return "SET " + res.TrimEnd(',');
    }

    public string ToDeleteMergeQuery() => "DELETE";

    // T-SQL escapes a single quote by DOUBLING it. The previous `\'` is MySQL syntax and SQL Server
    // does not treat a backslash as an escape, so N'Сім\'я' terminated the literal after «Сім\» and
    // left «я'» as a syntax error — every value containing an apostrophe broke the statement, and the
    // Ukrainian catalog is full of them (Сім'я, Пам'ятні). Doubling is also what makes the
    // interpolation safe: a value can no longer close its own literal and append SQL.
    private string Sanitize(string input) => string.IsNullOrEmpty(input) ? input : input.Replace("'", "''").Trim();

    private string ExcludeIdColumn(string cols) => cols.Split(",").Where(x => x is not "[Id]" and not "Id").Join(",");
}
