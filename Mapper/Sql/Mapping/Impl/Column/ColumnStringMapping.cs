using System.Data;
using System.Reflection;

namespace Sencilla.Infrastructure.SqlMapper.Mapping.Impl.Column
{
    public class ColumnStringMapping<TEntity> : ColumnMapping<TEntity, string>
    {
        public ColumnStringMapping(ITableMapping table, PropertyInfo info, string name, string fieldName) : base(table, info, name, fieldName) { }
        protected ColumnStringMapping() { }
        protected override string ReadValue(IDataReader reader, int index)
        {
            return reader.IsDBNull(index) ? null : reader.GetString(index);
        }
        public override IColumnMapping<TEntity> Clone(ITableMapping table)
        {
            return new ColumnStringMapping<TEntity>().Clone(this, table);
        }
    }
}
