using Sencilla.Infrastructure.SqlMapper.Mapping;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;

namespace Sencilla.Infrastructure.SqlMapper.Impl.Expression
{
    class ExpressionParser<TEntity>
    {
        private readonly Dictionary<string, ExpressionParseResult> parseResult = new Dictionary<string, ExpressionParseResult>();

        public IList<ExpressionParseResult> Parse(ITableMapping table, Expression<Func<TEntity, object>> expression)
        {
            Clear();
            ParseExpression(expression.Body, ref table);

            return parseResult.Values.ToList();
        }

        private void Clear()
        {
            parseResult.Clear();
        }

        private void ParseExpression(System.Linq.Expressions.Expression exp, ref ITableMapping table)
        {
            switch (exp.NodeType)
            {
                case ExpressionType.Convert:
                case ExpressionType.ConvertChecked:
                    ParseExpression((exp as UnaryExpression).Operand, ref table);
                    break;

                case ExpressionType.MemberAccess:
                    var memberAccess = (exp as MemberExpression);

                    ITableMapping accessCurrTable = table;
                    ParseExpression(memberAccess.Expression, ref accessCurrTable);

                    var name = memberAccess.Member.Name;
                    var property = accessCurrTable.GetProperty(name);
                    if (property == null)
                        throw new ArgumentException($"There is no navigation property '{name}' for entity {typeof(TEntity).FullName}");

                    table = property.ForeignTable;

                    AddToResult($"{table.Alias}_{name}", property);
                    break;

                case ExpressionType.Lambda:
                    ParseExpression((exp as LambdaExpression).Body, ref table);
                    break;

                case ExpressionType.Call:
                    var args = (exp as MethodCallExpression).Arguments;

                    var callCurrTable = table;
                    ParseExpression(args[0], ref callCurrTable);

                    if (args.Count > 1)
                        ParseExpression(args[1], ref callCurrTable);
                    break;

                default:
                    // Do nothing 
                    break;
            }
        }

        private void AddToResult(string uniqueKey, IPropertyMapping property)
        {
            if (!parseResult.ContainsKey(uniqueKey))
            {
                parseResult.Add(uniqueKey, new ExpressionParseResult(uniqueKey, property));
            }
        }
    }
}
