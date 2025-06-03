using System.Data;
using System.Reflection;

namespace Sencilla.Infrastructure.SqlMapper.Mapping.Impl.Column
{
    public class ColumnDecimalMapping<TEntity> : ColumnMapping<TEntity, decimal>
    {
        public ColumnDecimalMapping(ITableMapping table, PropertyInfo info, string name, string fieldName) : base(table, info, name, fieldName) { }
        protected ColumnDecimalMapping() { }
        protected override decimal ReadValue(IDataReader reader, int index)
        {
            return reader.GetDecimal(index);
        }

        public override IColumnMapping<TEntity> Clone(ITableMapping table)
        {
            return new ColumnDecimalMapping<TEntity>().Clone(this, table);
        }
    }

    public class ColumnDecimalNullMapping<TEntity> : ColumnMapping<TEntity, decimal?>
    {
        public ColumnDecimalNullMapping(ITableMapping table, PropertyInfo info, string name, string fieldName) : base(table, info, name, fieldName) { }
        protected ColumnDecimalNullMapping() { }

        protected override decimal? ReadValue(IDataReader reader, int index)
        {
            return reader.IsDBNull(index) ? (decimal?)null : reader.GetDecimal(index);
        }

        public override IColumnMapping<TEntity> Clone(ITableMapping table)
        {
            return new ColumnDecimalNullMapping<TEntity>().Clone(this, table);
        }
    }
}
