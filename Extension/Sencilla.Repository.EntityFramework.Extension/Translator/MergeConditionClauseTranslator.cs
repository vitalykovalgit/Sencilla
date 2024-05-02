namespace Sencilla.Repository.EntityFramework.Extension;

public class MergeConditionClauseTranslator : BaseQueryTranslator
{
    protected internal override void Translate(Expression e)
    {
        SB = new StringBuilder();
        Visit(e);

        Condition = PrepareCondition(SB.ToString());

        SB.Clear();
    }

    protected override Expression VisitMember(MemberExpression m)
    {
        if (m.Expression != null && m.Expression.NodeType == ExpressionType.Parameter)
        {
            SB.Append(ToMultipleParametersString(m.ToString().Split(".")));
            return m;
        }

        throw new NotSupportedException(string.Format("The member '{0}' is not supported", m.Member.Name));
    }

    private string PrepareCondition(string mergeCols)
    {
        var res = string.Empty;
        var cols = mergeCols.TrimEnd(',').Split(",");

        foreach (var c in cols)
        {
            res += $"t.{c} = s.{c} AND " ;
        }

        var trimmer = "AND ";
        return res.Substring(0, res.Length - trimmer.Length);
    }

    private string ToMultipleParametersString(string[] splited) => $"[{splited[1]}],";
}
