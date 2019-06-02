namespace Sencilla.Infrastructure.SqlMapper.Impl.Expression
{
    public abstract class BaseExpression : ISqlExpression
    {
        public virtual string Sql { get; protected set; }

        public override string ToString()
        {
            return Sql;
        }
    }
}
