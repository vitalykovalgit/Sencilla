using System.Collections.Generic;
using System.Data.Common;

namespace System.Data.SqlClient
{
    /// <inheritdoc />
    /// <summary>
    /// </summary>
    public class SqlServerDbProviderParam : IDbProviderParam
    {
        private readonly Dictionary<Type, SqlDbType> typeMap = new Dictionary<Type, SqlDbType>();

        public SqlServerDbProviderParam()
        {
            typeMap[typeof(byte)] = SqlDbType.TinyInt;
            typeMap[typeof(short)] = SqlDbType.SmallInt;
            typeMap[typeof(int)] = SqlDbType.Int;
            typeMap[typeof(long)] = SqlDbType.BigInt;
            typeMap[typeof(float)] = SqlDbType.Real;
            typeMap[typeof(double)] = SqlDbType.Float;
            typeMap[typeof(decimal)] = SqlDbType.Decimal;
            typeMap[typeof(bool)] = SqlDbType.Bit;
            typeMap[typeof(string)] = SqlDbType.NVarChar;
            typeMap[typeof(char[])] = SqlDbType.NVarChar;
            typeMap[typeof(char)] = SqlDbType.NVarChar;
            typeMap[typeof(Guid)] = SqlDbType.UniqueIdentifier;
            typeMap[typeof(DateTime)] = SqlDbType.DateTime2;
            typeMap[typeof(DateTimeOffset)] = SqlDbType.DateTimeOffset;
            typeMap[typeof(TimeSpan)] = SqlDbType.Time;
            typeMap[typeof(byte[])] = SqlDbType.VarBinary;
        }

        public DbParameter Create()
        {
            return new SqlParameter();
        }

        public DbParameter Create(string name, object value)
        {
            return new SqlParameter(name, value ?? DBNull.Value);
        }

        public DbParameter Create(string name, object value, Type type)
        {
            var valueType = GetTypeOfValue(type);
            var sqlValueType = MapToDbType(valueType);
            var sqlValue = value;

            if (sqlValue != null)
                sqlValue = Convert.ChangeType(sqlValue, valueType);
            else
                sqlValue = DBNull.Value;

            var param = new SqlParameter(name, sqlValueType);
            param.Value = sqlValue;

            return param;
        }

        public DbParameter CreateStructured(string typeName)
        {
            return new SqlParameter
            {
                SqlDbType = SqlDbType.Structured,
                TypeName = typeName
            };
        }

        #region Private methods

        private Type GetTypeOfValue(Type valueType)
        {
            var type = valueType;
            var underlyingType = Nullable.GetUnderlyingType(type);

            type = underlyingType ?? type;
            if (type.IsEnum)
                underlyingType = Enum.GetUnderlyingType(type);

            type = underlyingType ?? type;

            return type;
        }

        private SqlDbType MapToDbType(Type type)
        {
            if (typeMap.ContainsKey(type)) return typeMap[type];

            throw new ArgumentException($"Can't convert type {type.FullName} to {nameof(SqlDbType)}");
        }

        #endregion
    }
}
