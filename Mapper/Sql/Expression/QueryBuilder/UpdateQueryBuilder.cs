using Sencilla.Infrastructure.SqlMapper.Mapping;
using Sencilla.Infrastructure.SqlMapper.Mapping.Extention;

namespace Sencilla.Infrastructure.SqlMapper.Impl.Expression
{
    public class UpdateQueryBuilder<TEntity>
        : BaseQueryBuilder<TEntity>
        , ISqlWhereExpression
    {

        public UpdateQueryBuilder(DbContext context, ITableMapping<TEntity> table)
            : base(context, table)
        {
            UpdateExpr = $"UPDATE {Table.Reference} SET {Table.WriteColumns.ReferenceParams}";
        }

        public UpdateQueryBuilder<TEntity> Entity(TEntity entity)
        {
            Parameters.AddRange(Table.WriteColumns.ToParams(Context.DbProvider.Param, entity));
            return this;
        }

        public ISqlExpression Where { get; set; }

        public string UpdateExpr { get; set; }

        public override string Sql => $"{UpdateExpr} {Where}";        
    }
}
