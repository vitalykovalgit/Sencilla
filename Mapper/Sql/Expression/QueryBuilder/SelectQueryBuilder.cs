using Sencilla.Infrastructure.SqlMapper.Mapping;
using System;
using System.Collections.Generic;
using System.Linq.Expressions;

namespace Sencilla.Infrastructure.SqlMapper.Impl.Expression
{
    public interface ISqlWhereExpression : ISqlExpression
    {
        /// <summary>
        /// Where Sql Expression 
        /// </summary>
        ISqlExpression Where { get; set; }
    }

    public interface ISqlOrderByExpression : ISqlExpression
    {
        /// <summary>
        /// Order By Expression 
        /// </summary>
        ISqlExpression OrderBy { get; set; }
    }

    public interface ISqlSkipTakeExpression : ISqlExpression
    {
        /// <summary>
        /// Order By Expression 
        /// </summary>
        ISqlExpression SkipTake { get; set; }
    }

    public interface ISqlFromExpression : ISqlExpression
    {
        /// <summary>
        /// 
        /// </summary>
        ISqlExpression From { get; set; }
    }

    public interface ISqlSelectExpression<TEntity>
        : ISqlWhereExpression
        , ISqlOrderByExpression
        , ISqlSkipTakeExpression
        , ISqlExpressionExecutable<TEntity>
        , ISqlFromExpression 
    {
        /// <summary>
        /// Select expression 
        /// </summary>
        ISqlExpression Select { get; set; }
    }
    
    public class SelectQueryBuilder<TEntity> : BaseQueryBuilder<TEntity>, ISqlSelectExpression<TEntity>
    {
        private MultiExpresssion _joins;
        private Dictionary<string, JoinExpression> _joinsDictionary = new Dictionary<string, JoinExpression>();
        private readonly List<string> _columnsToSelect = new List<string>();

        public SelectQueryBuilder(DbContext context, ITableMapping<TEntity> table) 
            : base(context, table)
        {
            _columnsToSelect.Add(Table.Columns.Declarations);
            From = new SqlExpression($"FROM {Table}");
        }

        public SelectQueryBuilder<TEntity> Include(params Expression<Func<TEntity, object>>[] properties)
        {
            foreach (var expression in properties)
            {
                var parser = new ExpressionParser<TEntity>();

                var results = parser.Parse(Table, expression);
                foreach (var result in results)
                {
                    IncludeProperty(result.UniqueKey, result.Property);
                }
            }
            
            return this;
        }
        
        public override string Sql => $"SELECT {string.Join(",", _columnsToSelect)} {From} {_joins} {Where} {OrderBy} {SkipTake}";

        public ISqlExpression Where { get; set; }
        public ISqlExpression Select { get; set; }
        public ISqlExpression OrderBy { get; set; }
        public ISqlExpression SkipTake { get; set; }
        public ISqlExpression From { get; set; }

        #region Implementations 

        private void IncludeProperty(string joinKey, IPropertyMapping property)
        {
            if (!_joinsDictionary.ContainsKey(joinKey))
            {
                var join = new JoinExpression(property);
                (_joins ?? (_joins = new MultiExpresssion())).Add(join);
                _columnsToSelect.Add(property.ForeignTable.Columns.Declarations);

                _joinsDictionary[joinKey] = null;
            }
        }

        #endregion 
    }
}
