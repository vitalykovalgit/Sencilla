using System;
using System.Data;
using System.Reflection;

namespace Sencilla.Infrastructure.SqlMapper.Mapping.Impl.Column
{
    public class ColumnGuidMapping<TEntity> : ColumnMapping<TEntity, Guid>
    {
        public ColumnGuidMapping(ITableMapping table, PropertyInfo info, string name, string fieldName) : base(table, info, name, fieldName) { }
        protected ColumnGuidMapping() { }
        protected override Guid ReadValue(IDataReader reader, int index)
        {
            return reader.GetGuid(index);
        }
        public override IColumnMapping<TEntity> Clone(ITableMapping table)
        {
            return new ColumnGuidMapping<TEntity>().Clone(this, table);
        }
    }

    public class ColumnGuidNullMapping<TEntity> : ColumnMapping<TEntity, Guid?>
    {
        public ColumnGuidNullMapping(ITableMapping table, PropertyInfo info, string name, string fieldName) : base(table, info, name, fieldName) { }
        protected ColumnGuidNullMapping() { }
        protected override Guid? ReadValue(IDataReader reader, int index)
        {
            return reader.IsDBNull(index) ? (Guid?)null : reader.GetGuid(index);
        }
        public override IColumnMapping<TEntity> Clone(ITableMapping table)
        {
            return new ColumnGuidNullMapping<TEntity>().Clone(this, table);
        }
    }
}
