using System.Data;
using System.Reflection;

namespace Sencilla.Infrastructure.SqlMapper.Mapping.Impl.Column
{
    public class ColumnShortMapping<TEntity> : ColumnMapping<TEntity, short>
    {
        public ColumnShortMapping(ITableMapping table, PropertyInfo info, string name, string fieldName) : base(table, info, name, fieldName) { }
        protected ColumnShortMapping() { }
        protected override short ReadValue(IDataReader reader, int index)
        {
            return reader.GetInt16(index);
        }
        public override IColumnMapping<TEntity> Clone(ITableMapping table)
        {
            return new ColumnShortMapping<TEntity>().Clone(this, table);
        }
    }

    public class ColumnShortNullMapping<TEntity> : ColumnMapping<TEntity, short?>
    {
        public ColumnShortNullMapping(ITableMapping table, PropertyInfo info, string name, string fieldName) : base(table, info, name, fieldName) { }
        protected ColumnShortNullMapping() { }
        protected override short? ReadValue(IDataReader reader, int index)
        {
            return reader.IsDBNull(index) ? (short?)null : reader.GetInt16(index);
        }
        public override IColumnMapping<TEntity> Clone(ITableMapping table)
        {
            return new ColumnShortNullMapping<TEntity>().Clone(this, table);
        }
    }
}
