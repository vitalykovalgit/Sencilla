using System.Collections.Generic;
using System.Linq;

namespace Sencilla.Infrastructure.SqlMapper.Impl.Expression
{
    public class MultiExpresssion : BaseExpression
    {
        public MultiExpresssion(params ISqlExpression[] expressions) : this(" ", expressions)
        {
        }

        public MultiExpresssion(string separator, params ISqlExpression[] expressions)
        {
            Separator = separator;
            Children = new List<ISqlExpression>(expressions);
        }

        public MultiExpresssion Add(params ISqlExpression[] expressions)
        {
            Children.AddRange(expressions);
            return this;
        }

        public override string Sql => string.Join(Separator, Children.Select(c => c.Sql));

        protected List<ISqlExpression> Children { get; set; }

        protected string Separator { get; set; }

    }
}
