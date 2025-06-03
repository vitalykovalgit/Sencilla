using System.Data;
using System.Reflection;

namespace Sencilla.Infrastructure.SqlMapper.Mapping.Impl.Column
{
    public class ColumnBoolMapping<TEntity> : ColumnMapping<TEntity, bool>
    {
        public ColumnBoolMapping(ITableMapping table, PropertyInfo info, string name, string fieldName) : base(table, info, name, fieldName) { }
        protected ColumnBoolMapping() {}

        protected override bool ReadValue(IDataReader reader, int index)
        {
            return reader.GetBoolean(index);
        }

        public override IColumnMapping<TEntity> Clone(ITableMapping table)
        {
            return new ColumnBoolMapping<TEntity>().Clone(this, table);
        }
    }

    public class ColumnBoolNullMapping<TEntity> : ColumnMapping<TEntity, bool?>
    {
        public ColumnBoolNullMapping(ITableMapping table, PropertyInfo info, string name, string fieldName) : base(table, info, name, fieldName) { }
        protected ColumnBoolNullMapping() { }

        protected override bool? ReadValue(IDataReader reader, int index)
        {
            return reader.IsDBNull(index) ? (bool?)null : reader.GetBoolean(index);
        }

        public override IColumnMapping<TEntity> Clone(ITableMapping table)
        {
            return new ColumnBoolNullMapping<TEntity>().Clone(this, table);
        }
    }
}
