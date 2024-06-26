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

        if (p.PropertyType == typeof(Guid)
            || p.PropertyType == typeof(DateTime)
            || p.PropertyType == typeof(TimeSpan)
            || p.PropertyType == typeof(DateTimeOffset))
            return $"'{ov}'";

        if (p.PropertyType == typeof(bool))
            return bool.Parse(ov.ToString()) ? "1" : "0";

        return ov.ToString();
    }

    public string ToInsertMergeQuery(string eCols)
    {
        var withoutIdCols = ExcludeIdColumn(eCols);
        
        var insertVals = string.Empty;
        var cols = withoutIdCols.Split(",");

        foreach (var c in cols)
        {
            insertVals += $"s.{c},";
        }

        return "INSERT (" + withoutIdCols + ")" + Environment.NewLine + "VALUES (" + insertVals.TrimEnd(',') + ")";
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

    private string Sanitize(string input) => string.IsNullOrEmpty(input) ? input : input.Replace("'", @"\'").Trim();

    private string ExcludeIdColumn(string cols) => cols.Split(",").Where(x => x is not "[Id]" and not "Id").Join(",");
}
