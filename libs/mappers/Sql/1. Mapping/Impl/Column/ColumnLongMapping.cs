using System.Data;
using System.Reflection;

namespace Sencilla.Infrastructure.SqlMapper.Mapping.Impl.Column
{
    public class ColumnLongMapping<TEntity> : ColumnMapping<TEntity, long>
    {
        protected ColumnLongMapping() { }
        public ColumnLongMapping(ITableMapping table, PropertyInfo info, string name, string fieldName) : base(table, info, name, fieldName) { }
        protected override long ReadValue(IDataReader reader, int index) => reader.GetInt64(index);
        public override IColumnMapping<TEntity> Clone(ITableMapping table) => new ColumnLongMapping<TEntity>().Clone(this, table);
    }

    public class ColumnLongNullMapping<TEntity> : ColumnMapping<TEntity, long?>
    {
        protected ColumnLongNullMapping() { }
        public ColumnLongNullMapping(ITableMapping table, PropertyInfo info, string name, string fieldName) : base(table, info, name, fieldName) { }
        protected override long? ReadValue(IDataReader reader, int index) => reader.IsDBNull(index) ? (long?)null : reader.GetInt64(index);
        public override IColumnMapping<TEntity> Clone(ITableMapping table) => new ColumnLongNullMapping<TEntity>().Clone(this, table);
    }
}
