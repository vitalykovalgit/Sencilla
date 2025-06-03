using System.Data;
using System.IO;
using System.Reflection;

namespace Sencilla.Infrastructure.SqlMapper.Mapping.Impl.Column
{
    public class ColumnBytesMapping<TEntity> : ColumnMapping<TEntity, byte[]>
    {
        public ColumnBytesMapping(ITableMapping table, PropertyInfo info, string name, string fieldName) : base(table, info, name, fieldName) { }
        protected ColumnBytesMapping() { }

        protected override byte[] ReadValue(IDataReader reader, int index)
        {
            using (var stream = new MemoryStream())
            {
                var buffer = new byte[8192];
                var offset = 0L;
                var read = 0L;

                if (reader.IsDBNull(index)) return null;

                while((read = reader.GetBytes(index, offset, buffer, 0, buffer.Length)) > 0)
                {
                    stream.Write(buffer, 0, (int)read);
                    offset += read;
                };
                
                return stream.ToArray();
            }
        }

        public override IColumnMapping<TEntity> Clone(ITableMapping table)
        {
            return new ColumnBytesMapping<TEntity>().Clone(this, table);
        }
    }
}
