namespace Sencilla.Repository.EntityFramework.Extension;

public class MergeUpdateClauseTranslator : BaseQueryTranslator
{
    protected internal override void Translate(Expression e)
    {
        SB = new StringBuilder();
        Visit(e);
        Condition = "SET " + SB.ToString().TrimEnd(',');
        SB.Clear();
    }

    protected override Expression VisitMember(MemberExpression m)
    {
        if (m.Expression != null && m.Expression.NodeType == ExpressionType.Parameter)
        {
            SB.Append(ToMergeUpdateSqlParamStr(m.ToString().Split(".")));
            return m;
        }

        throw new NotSupportedException(string.Format("The member '{0}' is not supported", m.Member.Name));
    }

    private string ToMergeUpdateSqlParamStr(string[] splited) => $"t.[{splited[1]}] = s.[{splited[1]}]" + Environment.NewLine + ",";
}
