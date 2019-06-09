using System.Data;
using System.Reflection;

namespace Sencilla.Infrastructure.SqlMapper.Mapping.Impl.Column
{
    public class ColumnUIntMapping<TEntity> : ColumnMapping<TEntity, uint>
    {
        protected ColumnUIntMapping() { }
        public ColumnUIntMapping(ITableMapping table, PropertyInfo info, string name, string fieldName) : base(table, info, name, fieldName) { }
        protected override uint ReadValue(IDataReader reader, int index) => (uint)reader.GetInt32(index);
        public override IColumnMapping<TEntity> Clone(ITableMapping t) => new ColumnUIntMapping<TEntity>().Clone(this, t);
    }

    public class ColumnUIntNullMapping<TEntity> : ColumnMapping<TEntity, uint?>
    {
        protected ColumnUIntNullMapping() { }
        public ColumnUIntNullMapping(ITableMapping table, PropertyInfo info, string name, string fieldName) : base(table, info, name, fieldName) { }

        protected override uint? ReadValue(IDataReader r, int idx) => r.IsDBNull(idx) ? (uint?)null : (uint)r.GetInt32(idx);
        public override IColumnMapping<TEntity> Clone(ITableMapping t) => new ColumnUIntNullMapping<TEntity>().Clone(this, t);
    }
}
