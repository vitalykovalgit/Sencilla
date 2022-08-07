using System.Linq.Expressions;

namespace Sencilla.Component.Security
{
    public class ConstraintExpressionComplex: ConstraintExpression
    {
        protected List<IConstraintExpression> Expressions { get; } = new List<IConstraintExpression>();

        public override void Add(params IConstraintExpression[] exprs)
        {
            Expressions.AddRange(exprs);
        }

        public override Expression<Func<TEntity, bool>> ToExpression<TEntity>()
        {
            switch(Operator)
            {
                case "&":
                    break;
                case "|":
                    break;
            }

            return null;
        }
    }
}
