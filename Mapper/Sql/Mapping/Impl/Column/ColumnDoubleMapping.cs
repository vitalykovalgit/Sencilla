using System.Data;
using System.Reflection;

namespace Sencilla.Infrastructure.SqlMapper.Mapping.Impl.Column
{
    public class ColumnDoubleMapping<TEntity> : ColumnMapping<TEntity, double>
    {
        public ColumnDoubleMapping(ITableMapping table, PropertyInfo info, string name, string fieldName) : base(table, info, name, fieldName) { }
        protected ColumnDoubleMapping() { }
        protected override double ReadValue(IDataReader reader, int index)
        {
            return reader.GetDouble(index);
        }
        public override IColumnMapping<TEntity> Clone(ITableMapping table)
        {
            return new ColumnDoubleMapping<TEntity>().Clone(this, table);
        }
    }

    public class ColumnDoubleNullMapping<TEntity> : ColumnMapping<TEntity, double?>
    {
        public ColumnDoubleNullMapping(ITableMapping table, PropertyInfo info, string name, string fieldName) : base(table, info, name, fieldName) { }
        protected ColumnDoubleNullMapping() { }
        protected override double? ReadValue(IDataReader reader, int index)
        {
            return reader.IsDBNull(index) ? (double?)null : reader.GetDouble(index);
        }
        public override IColumnMapping<TEntity> Clone(ITableMapping table)
        {
            return new ColumnDoubleNullMapping<TEntity>().Clone(this, table);
        }
    }
}
