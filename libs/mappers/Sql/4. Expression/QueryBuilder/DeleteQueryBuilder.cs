using Sencilla.Infrastructure.SqlMapper.Mapping;
using System.Linq.Expressions;

namespace Sencilla.Infrastructure.SqlMapper.Impl.Expression
{
    public class DeleteQueryBuilder<TEntity>
        : BaseQueryBuilder<TEntity>
        , ISqlWhereExpression
    {

        public DeleteQueryBuilder(DbContext context, ITableMapping<TEntity> table)
            : base(context, table)
        {
            DeleteExpr = $"DELETE FROM {Table.Reference}";
        }

        public ISqlExpression Where { get; set; }
        public string DeleteExpr { get; set; }

        public override string Sql => $"{DeleteExpr} {Where}";

        
        public void ParseExpression(System.Linq.Expressions.Expression expr)
        {
            switch (expr.NodeType)
            {
                case ExpressionType.Equal:
                    ParseEqual((BinaryExpression)expr);
                    break;
                case ExpressionType.MemberAccess:
                    var membrExpr = (MemberExpression)expr;
                    if (membrExpr.Expression.NodeType == ExpressionType.Parameter)
                    {
                        var colName = membrExpr.Member.Name;
                    }
                    else if (membrExpr.Expression.NodeType == ExpressionType.Constant)
                    {
                        var v = System.Linq.Expressions.Expression.Lambda(membrExpr);
                        var d = v.Compile();
                        var z = d.DynamicInvoke();
                        var value = ((ConstantExpression)membrExpr.Expression).Value;
                    }
                    break;
                case ExpressionType.Constant:
                    var constExpr = (ConstantExpression) expr;
                    var val = constExpr.Value;

                    break;
            }
        }


        public void ParseEqual(BinaryExpression body)
        {
            ParseExpression(body.Left);
            ParseExpression(body.Right);
        }
    }
}
