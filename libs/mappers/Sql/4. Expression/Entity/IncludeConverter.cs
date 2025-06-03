using Sencilla.Infrastructure.SqlMapper.Mapping;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;

namespace Sencilla.Infrastructure.SqlMapper.Impl.Expression
{
    public class IncludeConverter<TEntity>
    {
        public IList<Include> ToIncludes(ITableMapping table, params Expression<Func<TEntity, object>>[] properties)
        {
            var parseResults = ParseExpressions(table, properties);

            return ConvertToIncludes(parseResults);
        }

        private static IList<ExpressionParseResult> ParseExpressions(ITableMapping table, params Expression<Func<TEntity, object>>[] properties)
        {
            var parser = new ExpressionParser<TEntity>();
            var parseResultsMap = new Dictionary<string, ExpressionParseResult>();

            foreach (var expression in properties)
            {
                var expressionsResults = parser.Parse(table, expression);
                foreach (var result in expressionsResults)
                {
                    if (parseResultsMap.ContainsKey(result.UniqueKey)) continue;

                    parseResultsMap.Add(result.UniqueKey, result);
                }
            }

            return parseResultsMap.Values.ToList();
        }

        private static IList<Include> ConvertToIncludes(IList<ExpressionParseResult> parseResults)
        {
            var includes = new List<Include>();

            foreach (var parseResult in parseResults)
            {
                var parentColumn = parseResult.Property.ForeignKeyColumn;
                var joinedColumn = parseResult.Property.JoinOnColumn;

                includes.Add(new Include
                {
                    ParentTable = TableName(parentColumn),
                    ParentKey = ColumnName(parentColumn),
                    JoinedTable = TableName(joinedColumn),
                    JoinedKey = ColumnName(joinedColumn)
                });
            }

            return includes;
        }

        private static string TableName(IColumnMapping column)
        {
            return string.IsNullOrEmpty(column.TableSchema) ? column.TableName : $"{column.TableSchema}.{column.TableName}";
        }

        private static string ColumnName(IColumnMapping column)
        {
            return column.Name;
        }
    }
}
