using System;
using System.Data;
using System.Reflection;

namespace Sencilla.Infrastructure.SqlMapper.Mapping.Impl.Column
{
    public class ColumnEnumMapping<TEntity, TEnum> : ColumnMapping<TEntity, TEnum>
        where TEnum : struct
    {
        public ColumnEnumMapping(ITableMapping table, PropertyInfo info, string name, string fieldName) 
            : base(table, info, name, fieldName)
        {
        }

        protected ColumnEnumMapping() { }

        protected override TEnum ReadValue(IDataReader reader, int index)
        {
            var enumType = typeof(TEnum);
            var enumUnderlyingType = Enum.GetUnderlyingType(enumType);

            object value = null;

            if (enumUnderlyingType == typeof(byte))
            {
                value = reader.GetByte(index);
            }
            else if (enumUnderlyingType == typeof(short))
            {
                value = reader.GetInt16(index);
            }
            else if (enumUnderlyingType == typeof(int))
            {
                value = reader.GetInt32(index);
            }
            else if (enumUnderlyingType == typeof(long))
            {
                value = reader.GetInt64(index);
            }
            else throw new InvalidOperationException($"Unexpected enum type: {enumUnderlyingType.FullName}");

            return (TEnum)value;
        }

        public override IColumnMapping<TEntity> Clone(ITableMapping table)
        {
            return new ColumnEnumMapping<TEntity, TEnum>().Clone(this, table);
        }
    }

    public class ColumnEnumNullMapping<TEntity, TEnum> : ColumnMapping<TEntity, TEnum?>
        where TEnum: struct
    {
        public ColumnEnumNullMapping(ITableMapping table, PropertyInfo info, string name, string fieldName) 
            : base(table, info, name, fieldName)
        {
        }

        protected ColumnEnumNullMapping() { }

        protected override TEnum? ReadValue(IDataReader reader, int index)
        {
            if (reader.IsDBNull(index)) return null;

            var enumType = typeof(TEnum);
            var enumUnderlyingType = Enum.GetUnderlyingType(enumType);

            object value = null;

            if (enumUnderlyingType == typeof(byte))
            {
                value = reader.GetByte(index);
            }
            else if (enumUnderlyingType == typeof(short))
            {
                value = reader.GetInt16(index);
            }
            else if (enumUnderlyingType == typeof(int))
            {
                value = reader.GetInt32(index);
            }
            else if (enumUnderlyingType == typeof(long))
            {
                value = reader.GetInt64(index);
            }
            else throw new InvalidOperationException($"Unexpected enum type: {enumUnderlyingType.FullName}");

            return (TEnum)value;
        }

        public override IColumnMapping<TEntity> Clone(ITableMapping table)
        {
            return new ColumnEnumNullMapping<TEntity, TEnum>().Clone(this, table);
        }
    }
}
