using Sencilla.Infrastructure.SqlMapper.Mapping;

namespace Sencilla.Infrastructure.SqlMapper.Impl.Expression
{
    public class JoinExpression : BaseExpression
    {
        private readonly IPropertyMapping Property;

        public JoinExpression(IPropertyMapping property)
        {
            Property = property;
        }

        public override string Sql => $"LEFT JOIN {Property.ForeignTable} ON {Property.ForeignKeyColumn.AliasReference} = {Property.JoinOnColumn.AliasReference}";
    }
}
