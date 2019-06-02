using System.Data;
using System.Reflection;

namespace Sencilla.Infrastructure.SqlMapper.Mapping.Impl.Column
{
    public class ColumnLongMapping<TEntity> : ColumnMapping<TEntity, long>
    {
        public ColumnLongMapping(ITableMapping table, PropertyInfo info, string name, string fieldName) : base(table, info, name, fieldName) { }
        protected ColumnLongMapping() { }
        protected override long ReadValue(IDataReader reader, int index)
        {
            return reader.GetInt64(index);
        }

        public override IColumnMapping<TEntity> Clone(ITableMapping table)
        {
            return new ColumnLongMapping<TEntity>().Clone(this, table);
        }
    }

    public class ColumnLongNullMapping<TEntity> : ColumnMapping<TEntity, long?>
    {
        public ColumnLongNullMapping(ITableMapping table, PropertyInfo info, string name, string fieldName) : base(table, info, name, fieldName) { }
        protected ColumnLongNullMapping() { }
        protected override long? ReadValue(IDataReader reader, int index)
        {
            return reader.IsDBNull(index) ? (long?)null : reader.GetInt64(index);
        }

        public override IColumnMapping<TEntity> Clone(ITableMapping table)
        {
            return new ColumnLongNullMapping<TEntity>().Clone(this, table);
        }
    }
}
