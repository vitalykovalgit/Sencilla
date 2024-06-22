namespace Sencilla.Repository.EntityFramework.Extension;

public class BaseQueryTranslator : ExpressionVisitor
{
    protected StringBuilder SB;
    public string Condition = string.Empty;

    protected internal virtual void Translate(Expression e)
    {
        SB = new StringBuilder();
        Visit(e);
        var matchCol = SB.ToString();
        Condition = $"t.[{matchCol}] = s.[{matchCol}]";
        SB.Clear();
    }

    protected override Expression VisitMember(MemberExpression m)
    {
        if (m.Expression != null && m.Expression.NodeType == ExpressionType.Parameter)
        {
            SB.Append(m.Member.Name);
            return m;
        }

        throw new NotSupportedException(string.Format("The member '{0}' is not supported", m.Member.Name));
    }
}
