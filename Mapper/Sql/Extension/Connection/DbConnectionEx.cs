using System.Collections.Generic;

namespace System.Data.Common
{
    /// <summary>
    /// 
    /// </summary>
    public static partial class DbConnectionEx
    {   
        /// <summary>
        /// 
        /// </summary>
        /// <param name="connection"></param>
        /// <param name="type"></param>
        /// <param name="sql"></param>
        /// <param name="transaction"></param>
        /// <param name="timeout"></param>
        /// <param name="parameters"></param>
        /// <returns></returns>
        public static DbCommand CreateCommand(this DbConnection connection, CommandType type, string sql, DbTransaction transaction, int? timeout, params DbParameter[] parameters)
        {
            return connection.CreateCommand(type, sql, transaction, timeout, (IEnumerable<DbParameter>)parameters);
        }

        public static DbCommand CreateCommand(this DbConnection connection, CommandType type, string sql, DbTransaction transaction, int? timeout, IEnumerable<DbParameter> parameters)
        {
            var cmd = connection.CreateCommand();
            cmd.CommandText = sql;
            cmd.CommandType = type;

            if (timeout.HasValue)
                cmd.CommandTimeout = timeout.Value;

            if (transaction != null)
                cmd.Transaction = transaction;

            if (parameters != null)
            {
                foreach (var p in parameters)
                    cmd.Parameters.Add(p);
            }

            return cmd;
        }
    }
}
