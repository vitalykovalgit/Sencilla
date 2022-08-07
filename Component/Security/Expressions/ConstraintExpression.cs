using System.Linq.Expressions;
using System.Reflection;

namespace Sencilla.Component.Security
{
    /// <summary>
    /// Security constraint
    /// </summary>
    public class ConstraintExpression : IConstraintExpression
    {
        public string Field { get; set; }
        public object Value { get; set; }
        public string Operator { get; set; }

        public virtual void Add(params IConstraintExpression[] exps)
        {
            // do nothing here for simple expression 
        }

        public virtual Expression<Func<TEntity, bool>>? ToExpression<TEntity>()
        {
            var param = Expression.Parameter(typeof(TEntity), "e");
            var property = Expression.Property(param, Field);
            var value = Expression.Constant(Value);

            Expression? compare = null;
            switch (Operator)
            {
                case "=":
                    compare = Expression.Equal(property, value);
                    break;
                case "!=":
                    compare = Expression.NotEqual(property, value);
                    break;
                case ">":
                    compare = Expression.GreaterThan(property, value);
                    break;
                case ">=":
                    compare = Expression.GreaterThanOrEqual(property, value);
                    break;
                case "<":
                    compare = Expression.LessThan(property, value);
                    break;
                case "<=":
                    compare = Expression.LessThanOrEqual(property, value);
                    break;
                case "in":
                    MethodInfo method = typeof(Enumerable).GetMethod("Contains", BindingFlags.Static | BindingFlags.Public);
                    compare = Expression.Call(null, method, property, value);
                    break;
                case "like":
                    // TODO: implement
                    // var customers = context.Customers.Where(e => EF.Functions.Like(e.Name, "a%"));
                    break;
            }
            
            if (compare != null)
            {
                var expr = Expression.Lambda<Func<TEntity, bool>>(compare, param);
                return expr;
            }
            return null;
        }
    }
}
