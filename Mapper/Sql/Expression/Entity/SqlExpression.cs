namespace Sencilla.Infrastructure.SqlMapper.Impl.Expression
{
    public sealed class SqlExpression : BaseExpression
    {
        public SqlExpression(string expression)
        {
            Sql = expression;
        }
    }
}
