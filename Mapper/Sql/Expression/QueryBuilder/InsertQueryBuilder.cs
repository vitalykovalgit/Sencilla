using Sencilla.Infrastructure.SqlMapper.Mapping;
using Sencilla.Infrastructure.SqlMapper.Mapping.Extention;
using Sencilla.Infrastructure.SqlMapper.Provider;

namespace Sencilla.Infrastructure.SqlMapper.Impl.Expression
{
    public class InsertQueryBuilder<TEntity>
        : BaseQueryBuilder<TEntity>
    {
        public InsertQueryBuilder(DbContext context, ITableMapping<TEntity> table)
            : base(context, table)
        {
            if (context.DbProvider.DbType == DbProviderType.SqlServer)
                InsertExpr = $"INSERT INTO {Table.Reference}({Table.WriteColumns.Names}) OUTPUT inserted.[{Table.PrimaryKey.Name}] VALUES ({Table.WriteColumns.Params})";
            else 
                InsertExpr = $"INSERT INTO {Table.Reference}({Table.WriteColumns.Names}) VALUES ({Table.WriteColumns.Params})";
        }

        public InsertQueryBuilder<TEntity> Entity(TEntity entity)
        {
            Parameters.AddRange(Table.WriteColumns.ToParams(Context.DbProvider.Param, entity));
            return this;
        }

        public string InsertExpr { get; set; }

        public override string Sql => $"{InsertExpr}";        
    }
}
