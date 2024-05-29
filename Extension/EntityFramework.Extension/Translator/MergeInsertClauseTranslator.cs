namespace Sencilla.Repository.EntityFramework.Extension;

public class MergeInsertClauseTranslator : BaseQueryTranslator
{
    protected internal override void Translate(Expression e)
    {
        SB = new StringBuilder();
        Visit(e);

        var mc = SB.ToString().TrimEnd(',');
        Condition = "INSERT (" + mc + ")" + Environment.NewLine + "VALUES (" + PrepareMergeValues(mc) + ")";

        SB.Clear();
    }

    protected override Expression VisitMember(MemberExpression m)
    {
        if (m.Expression != null && m.Expression.NodeType == ExpressionType.Parameter)
        {
            if (!m.ToString().Equals("Id"))
                SB.Append(ToMergeInsertSqlParamStr(m.ToString().Split(".")));
            return m;
        }

        throw new NotSupportedException(string.Format("The member '{0}' is not supported", m.Member.Name));
    }

    private string PrepareMergeValues(string mergeCols)
    {
        var res = string.Empty;
        var cols = mergeCols.Split(",");

        foreach (var c in cols)
        {
            res += $"s.{c},";
        }

        return res.TrimEnd(',');
    }

    private string ToMergeInsertSqlParamStr(string[] splited) => $"[{splited[1]}],";
}
