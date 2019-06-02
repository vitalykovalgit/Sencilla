using System.Data;
using System.Reflection;

namespace Sencilla.Infrastructure.SqlMapper.Mapping.Impl.Column
{
    public class ColumnIntMapping<TEntity> : ColumnMapping<TEntity, int>
    {
        public ColumnIntMapping(ITableMapping table, PropertyInfo info, string name, string fieldName) : base(table, info, name, fieldName) { }
        protected ColumnIntMapping() { }
        protected override int ReadValue(IDataReader reader, int index)
        {
            return reader.GetInt32(index);
        }
        public override IColumnMapping<TEntity> Clone(ITableMapping table)
        {
            return new ColumnIntMapping<TEntity>().Clone(this, table);
        }
    }

    public class ColumnIntNullMapping<TEntity> : ColumnMapping<TEntity, int?>
    {
        public ColumnIntNullMapping(ITableMapping table, PropertyInfo info, string name, string fieldName) : base(table, info, name, fieldName) { }
        protected ColumnIntNullMapping() { }
        protected override int? ReadValue(IDataReader reader, int index)
        {
            return reader.IsDBNull(index) ? (int?)null : reader.GetInt32(index);
        }
        public override IColumnMapping<TEntity> Clone(ITableMapping table)
        {
            return new ColumnIntNullMapping<TEntity>().Clone(this, table);
        }
    }
}
