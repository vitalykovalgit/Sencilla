using System;
using System.Data;
using System.Reflection;

namespace Sencilla.Infrastructure.SqlMapper.Mapping.Impl.Column
{
    public class ColumnDateTimeMapping<TEntity> : ColumnMapping<TEntity, DateTime>
    {
        public ColumnDateTimeMapping(ITableMapping table, PropertyInfo info, string name, string fieldName) : base(table, info, name, fieldName) { }
        protected ColumnDateTimeMapping() { }

        protected override DateTime ReadValue(IDataReader reader, int index)
        {
            return reader.GetDateTime(index);
        }

        public override IColumnMapping<TEntity> Clone(ITableMapping table)
        {
            return new ColumnDateTimeMapping<TEntity>().Clone(this, table);
        }
    }

    public class ColumnDateTimeNullMapping<TEntity> : ColumnMapping<TEntity, DateTime?>
    {
        public ColumnDateTimeNullMapping(ITableMapping table, PropertyInfo info, string name, string fieldName) : base(table, info, name, fieldName) { }
        protected ColumnDateTimeNullMapping() { }

        protected override DateTime? ReadValue(IDataReader reader, int index)
        {
            return reader.IsDBNull(index) ? (DateTime?)null : reader.GetDateTime(index);
        }

        public override IColumnMapping<TEntity> Clone(ITableMapping table)
        {
            return new ColumnDateTimeNullMapping<TEntity>().Clone(this, table);
        }
    }
}
