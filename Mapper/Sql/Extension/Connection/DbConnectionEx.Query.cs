using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace System.Data.Common
{
    /// <summary>
    /// 
    /// </summary>
    public static partial class DbConnectionEx
    {
        /*
         *  No async/await
         */
        public static int ExecuteQuery(this DbConnection connection, string sql, IEnumerable<DbParameter> parameters = null)
        {
            return connection.ExecuteQuery(sql, null, null, parameters);
        }

        public static int ExecuteQuery(this DbConnection connection, string sql, DbTransaction transaction, IEnumerable<DbParameter> parameters = null)
        {
            return connection.ExecuteQuery(sql, transaction, null, parameters);
        }

        public static int ExecuteQuery(this DbConnection connection, string sql, DbTransaction transaction, int? timeout, IEnumerable<DbParameter> parameters = null)
        {
            using (var cmd = connection.CreateCommand(CommandType.Text, sql, transaction, timeout, parameters))
            {
                return cmd.ExecuteNonQuery();
            }
        }

        /*
         *  Asyncronous operations 
         */
        public static Task<int> ExecuteQueryAsync(this DbConnection connection, string sql, IEnumerable<DbParameter> parameters = null)
        {
            return connection.ExecuteQueryAsync(sql, null, null, parameters);
        }

        public static Task<int> ExecuteQueryAsync(this DbConnection connection, string sql, DbTransaction transaction, IEnumerable<DbParameter> parameters = null)
        {
            return connection.ExecuteQueryAsync(sql, transaction, null, parameters);
        }

        public static Task<int> ExecuteQueryAsync(this DbConnection connection, string sql, DbTransaction transaction, int? timeout, IEnumerable<DbParameter> parameters = null)
        {
            using (var cmd = connection.CreateCommand(CommandType.Text, sql, transaction, timeout, parameters))
            {
                return cmd.ExecuteNonQueryAsync();
            }
        }

        /*
         *  Asyncronous operations with cancellation token
         */
        public static Task<int> ExecuteQueryAsync(this DbConnection connection, string sql, CancellationToken token, params DbParameter[] parameters)
        {
            return connection.ExecuteQueryAsync(sql, null, null, token, parameters);
        }

        public static Task<int> ExecuteQueryAsync(this DbConnection connection, string sql, DbTransaction transaction, CancellationToken token, params DbParameter[] parameters)
        {
            return connection.ExecuteQueryAsync(sql, transaction, null, token, parameters);
        }

        public static Task<int> ExecuteQueryAsync(this DbConnection connection, string sql, DbTransaction transaction, int? timeout, CancellationToken token, params DbParameter[] parameters)
        {
            using (var cmd = connection.CreateCommand(CommandType.Text, sql, transaction, timeout, parameters))
            {
                return cmd.ExecuteNonQueryAsync(token);
            }
        }

    }
}
