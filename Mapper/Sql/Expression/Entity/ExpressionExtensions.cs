namespace Sencilla.Infrastructure.SqlMapper.Impl.Expression
{
    public static class ExpressionExtensions
    {
        /// <summary>
        /// 
        /// </summary>
        /// <typeparam name="TExpression"></typeparam>
        /// <param name="expr"></param>
        /// <param name="where"></param>
        /// <returns></returns>
        public static TExpression Where<TExpression>(this TExpression expr, string where) 
            where TExpression : ISqlWhereExpression
        {
            expr.Where = new SqlExpression($"WHERE {where}");
            return expr;
        }

        public static TExpression OrderBy<TExpression>(this TExpression expr, string orderBy)
            where TExpression : ISqlOrderByExpression
        {
            expr.OrderBy = new SqlExpression($"ORDER BY {orderBy}");
            return expr;
        }

        public static TExpression SkipTake<TExpression>(this TExpression expr, int skip, int take)
            where TExpression : ISqlSkipTakeExpression
        {
            (expr as BaseQueryBuilder)?.Add("@skip", skip);
            (expr as BaseQueryBuilder)?.Add("@take", take);
            expr.SkipTake = new SqlExpression($"OFFSET (@skip) ROWS FETCH NEXT (@take) ROWS ONLY");
            return expr;
        }
    }
}
