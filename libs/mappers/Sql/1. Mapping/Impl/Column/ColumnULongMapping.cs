using System.Data;
using System.Reflection;

namespace Sencilla.Infrastructure.SqlMapper.Mapping.Impl.Column
{
    public class ColumnULongMapping<TEntity> : ColumnMapping<TEntity, ulong>
    {
        protected ColumnULongMapping() { }
        public ColumnULongMapping(ITableMapping table, PropertyInfo info, string name, string fieldName) : base(table, info, name, fieldName) { }
        protected override ulong ReadValue(IDataReader reader, int index) => (ulong)reader.GetInt64(index);
        public override IColumnMapping<TEntity> Clone(ITableMapping table) => new ColumnULongMapping<TEntity>().Clone(this, table);
    }

    public class ColumnULongNullMapping<TEntity> : ColumnMapping<TEntity, ulong?>
    {
        protected ColumnULongNullMapping() { }
        public ColumnULongNullMapping(ITableMapping table, PropertyInfo info, string name, string fieldName) : base(table, info, name, fieldName) { }
        protected override ulong? ReadValue(IDataReader reader, int index) => reader.IsDBNull(index) ? (ulong?)null : (ulong)reader.GetInt64(index);
        public override IColumnMapping<TEntity> Clone(ITableMapping table) => new ColumnULongNullMapping<TEntity>().Clone(this, table);
    }
}
