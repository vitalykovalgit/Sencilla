using Sencilla.Infrastructure.SqlMapper.Mapping;

namespace Sencilla.Infrastructure.SqlMapper.Impl.Expression
{
    class ExpressionParseResult
    {
        public ExpressionParseResult(string uniqueKey, IPropertyMapping property)
        {
            UniqueKey = uniqueKey;
            Property = property;
        }

        public string UniqueKey { get; }

        public IPropertyMapping Property { get; }
    }
}
