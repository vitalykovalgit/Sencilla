using System.Data;
using System.Reflection;

namespace Sencilla.Infrastructure.SqlMapper.Mapping.Impl.Column
{
    public class ColumnFloatMapping<TEntity> : ColumnMapping<TEntity, float>
    {
        public ColumnFloatMapping(ITableMapping table, PropertyInfo info, string name, string fieldName) : base(table, info, name, fieldName) { }
        protected ColumnFloatMapping() { }
        protected override float ReadValue(IDataReader reader, int index)
        {
            return reader.GetFloat(index);
        }
        public override IColumnMapping<TEntity> Clone(ITableMapping table)
        {
            return new ColumnFloatMapping<TEntity>().Clone(this, table);
        }
    }

    public class ColumnFloatNullMapping<TEntity> : ColumnMapping<TEntity, float?>
    {
        public ColumnFloatNullMapping(ITableMapping table, PropertyInfo info, string name, string fieldName) : base(table, info, name, fieldName) { }
        protected ColumnFloatNullMapping() { }
        protected override float? ReadValue(IDataReader reader, int index)
        {
            return reader.IsDBNull(index) ? (float?)null : reader.GetFloat(index);
        }
        public override IColumnMapping<TEntity> Clone(ITableMapping table)
        {
            return new ColumnFloatNullMapping<TEntity>().Clone(this, table);
        }
    }
}
