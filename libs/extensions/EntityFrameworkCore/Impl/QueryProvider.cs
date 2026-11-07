using System.Globalization;

namespace Sencilla.Repository.EntityFramework.Extension;

public class QueryProvider
{
    /// <summary>The key column every hand-built statement matches, omits from an INSERT, and reads back.</summary>
    public const string KeyColumn = $"[{nameof(IEntity<int>.Id)}]";

    public string ToMergeQuery(Expression e, QueryClauseType t)
    {
        var tr = new TranslatorFactory().Get(t);
        tr.Translate(e);
        return tr.Condition;
    }

    /// <summary>
    /// Renders one property value as a T-SQL literal.
    ///
    /// Every branch tests the UNDERLYING type: `p.PropertyType` for a `DateTime?` is
    /// `Nullable&lt;DateTime&gt;`, which matched nothing and fell through to the bare `ToString()` at the
    /// end — emitting an unquoted date that is a syntax error. Enums fell through the same way and
    /// emitted their member NAME (`Machine`), which SQL Server reads as an identifier.
    ///
    /// The final fallback is culture-INVARIANT on purpose: under a comma-decimal culture such as
    /// uk-UA, `12.5m.ToString()` yields `12,5`, which splits one value into two inside the VALUES
    /// list — a column-count error, or silent column shifting on money when the counts happen to line
    /// up. This runs on the server, whose culture is not ours to assume.
    /// </summary>
    public string ToSqlParameterValue(PropertyInfo p, object ov)
    {
        if (ov == null)
            return $"NULL";

        var type = Nullable.GetUnderlyingType(p.PropertyType) ?? p.PropertyType;

        if (type == typeof(string))
            return $"N'{Sanitize(ov.ToString())}'";

        if (type == typeof(DateTime))
            return $"'{((DateTime)ov).ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture)}'";

        if (type == typeof(Guid)
            || type == typeof(TimeSpan)
            || type == typeof(DateTimeOffset))
            return $"'{ov}'";

        if (type == typeof(bool))
            return bool.Parse(ov.ToString()) ? "1" : "0";

        // Enums carry an integral value; the name is not a SQL literal.
        if (ov is Enum enumValue)
            return enumValue.ToString("D");

        // varbinary: ToString() on a byte[] yields the literal text "System.Byte[]".
        if (ov is byte[] bytes)
            return bytes.Length == 0 ? "0x" : "0x" + Convert.ToHexString(bytes);

        return Convert.ToString(ov, CultureInfo.InvariantCulture)!;
    }

    /// <summary>
    /// Builds the MERGE's `WHEN NOT MATCHED THEN INSERT` clause. <paramref name="keyIsDatabaseGenerated"/>
    /// decides whether [Id] is named: a database-generated key must be left out, a client-supplied
    /// one (a string key) must be included or the insert writes NULL into a NOT NULL column.
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

    /// <summary>
    /// What the MERGE did to each row, and the row's key: the only place a key the database generated is ever known.
    /// <see cref="Microsoft.EntityFrameworkCore.EntityFrameworkCoreExtensions"/> reads the inserted ones back from it.
    /// </summary>
    // ponytail: OUTPUT without INTO, which SQL Server refuses on a table with an enabled trigger (Msg 334); no Sencilla
    // or Photoboost table has one. OUTPUT INTO a table variable, then SELECT from it, if one ever does.
    public string ToOutputMergeQuery() => $"OUTPUT $action, INSERTED.{KeyColumn}";

    // T-SQL escapes a single quote by DOUBLING it. The previous `\'` is MySQL syntax and SQL Server
    // does not treat a backslash as an escape, so N'Сім\'я' terminated the literal after «Сім\» and
    // left «я'» as a syntax error — every value containing an apostrophe broke the statement, and the
    // Ukrainian catalog is full of them (Сім'я, Пам'ятні). Doubling is also what makes the
    // interpolation safe: a value can no longer close its own literal and append SQL.
    private string Sanitize(string input) => string.IsNullOrEmpty(input) ? input : input.Replace("'", "''").Trim();

    private string ExcludeIdColumn(string cols) => cols.Split(",").Where(x => x is not KeyColumn and not "Id").Join(",");
}
