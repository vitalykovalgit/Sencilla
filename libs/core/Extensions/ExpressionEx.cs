
namespace System.Linq.Expressions;

public static class ExpressionEx
{
    public static Expression<Func<T, bool>>? OrElse<T>(this Expression<Func<T, bool>>? expr, params Expression<Func<T, bool>>[] exprs)
    {
        if (expr == null)
            return expr;

        var parameter = Expression.Parameter(typeof(T));

        var leftVisitor = new ReplaceExpressionVisitor(expr.Parameters[0], parameter);
        var left = leftVisitor.Visit(expr.Body);

        var body = left;
        foreach (var e in exprs)
        {
            var rightVisitor = new ReplaceExpressionVisitor(e.Parameters[0], parameter);
            var right = rightVisitor.Visit(e.Body);
            body = Expression.OrElse(body, right);
        }

        return Expression.Lambda<Func<T, bool>>(body, parameter);
    }

    public static Expression<Func<T, bool>>? AndAlso<T>(this Expression<Func<T, bool>>? expr, params Expression<Func<T, bool>>[] exprs)
    {
        if (expr == null)
            return expr;

        var parameter = Expression.Parameter(typeof(T));

        var leftVisitor = new ReplaceExpressionVisitor(expr.Parameters[0], parameter);
        var left = leftVisitor.Visit(expr.Body);

        var body = left;
        foreach (var e in exprs)
        {
            var rightVisitor = new ReplaceExpressionVisitor(e.Parameters[0], parameter);
            var right = rightVisitor.Visit(e.Body);
            body = Expression.OrElse(body, right);
        }

        return Expression.Lambda<Func<T, bool>>(body, parameter);
    }
}

[DisableInjection]
public class ReplaceExpressionVisitor(Expression oldValue, Expression newValue) : ExpressionVisitor
{
    public override Expression Visit(Expression node) => node == oldValue ? newValue : base.Visit(node);
}
