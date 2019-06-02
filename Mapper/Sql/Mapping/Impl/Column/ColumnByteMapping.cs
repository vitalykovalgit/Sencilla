using System.Data;
using System.Reflection;

namespace Sencilla.Infrastructure.SqlMapper.Mapping.Impl.Column
{
    public class ColumnByteMapping<TEntity> : ColumnMapping<TEntity, byte>
    {
        public ColumnByteMapping(ITableMapping table, PropertyInfo info, string name, string fieldName) : base(table, info, name, fieldName) { }
        protected ColumnByteMapping() {}

        protected override byte ReadValue(IDataReader reader, int index)
        {
            return reader.GetByte(index);
        }

        public override IColumnMapping<TEntity> Clone(ITableMapping table)
        {
            return new ColumnByteMapping<TEntity>().Clone(this, table);
        }
    }

    public class ColumnByteNullMapping<TEntity> : ColumnMapping<TEntity, byte?>
    {
        public ColumnByteNullMapping(ITableMapping table, PropertyInfo info, string name, string fieldName) : base(table, info, name, fieldName) { }
        protected ColumnByteNullMapping() { }

        protected override byte? ReadValue(IDataReader reader, int index)
        {
            return reader.IsDBNull(index) ? (byte?)null : reader.GetByte(index);
        }

        public override IColumnMapping<TEntity> Clone(ITableMapping table)
        {
            return new ColumnByteNullMapping<TEntity>().Clone(this, table);
        }
    }
}
