using System;
using System.Collections.Generic;
using System.Data.Common;
using MySql.Data.MySqlClient;

namespace MySql.Data
{
    /// <inheritdoc />
    /// <summary>
    /// </summary>
    public class MySqlDbProviderParam : IDbProviderParam
    {
        private readonly Dictionary<Type, MySqlDbType> typeMap = new Dictionary<Type, MySqlDbType>();

        public MySqlDbProviderParam()
        {
            typeMap[typeof(byte)] = MySqlDbType.Byte;
            typeMap[typeof(short)] = MySqlDbType.Int16;
            typeMap[typeof(int)] = MySqlDbType.Int32;
            typeMap[typeof(long)] = MySqlDbType.Int64;
            typeMap[typeof(float)] = MySqlDbType.Float;
            typeMap[typeof(double)] = MySqlDbType.Double;
            typeMap[typeof(decimal)] = MySqlDbType.Decimal;
            typeMap[typeof(bool)] = MySqlDbType.Bit;
            typeMap[typeof(string)] = MySqlDbType.String;
            typeMap[typeof(char[])] = MySqlDbType.String;
            typeMap[typeof(char)] = MySqlDbType.VarChar;
            typeMap[typeof(Guid)] = MySqlDbType.String; // TODO: Review 
            typeMap[typeof(DateTime)] = MySqlDbType.DateTime;
            typeMap[typeof(DateTimeOffset)] = MySqlDbType.DateTime;
            typeMap[typeof(TimeSpan)] = MySqlDbType.Time;
            typeMap[typeof(byte[])] = MySqlDbType.VarBinary;
        }

        public DbParameter Create()
        {
            return new MySqlParameter();
        }

        public DbParameter Create(string name, object value)
        {
            return new MySqlParameter(name, value ?? DBNull.Value);
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

            var param = new MySqlParameter(name, sqlValueType);
            param.Value = sqlValue;

            return param;
        }

        public DbParameter CreateStructured(string typeName)
        {
            throw new NotImplementedException();
            //return new MySqlParameter
            //{
            //    SqlDbType = SqlDbType.Structured,
            //    TypeName = typeName,
            //    //DbType = 
            //};
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

        private MySqlDbType MapToDbType(Type type)
        {
            if (typeMap.ContainsKey(type)) return typeMap[type];

            throw new ArgumentException($"Can't convert type {type.FullName} to {nameof(MySqlDbType)}");
        }

        #endregion
    }
}
