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
         * No async await 
         */
        public static object ExecuteQueryScalar(this DbConnection connection, string sql, params DbParameter[] parameters)
        {
            return connection.ExecuteQueryScalar(sql, null, null, parameters);
        }

        public static object ExecuteQueryScalar(this DbConnection connection, string sql, DbTransaction transaction, params DbParameter[] parameters)
        {
            return connection.ExecuteQueryScalar(sql, transaction, null, parameters);
        }

        public static object ExecuteQueryScalar(this DbConnection connection, string sql, DbTransaction transaction, int? timeout, params DbParameter[] parameters)
        {
            using (var cmd = connection.CreateCommand(CommandType.Text, sql, transaction, timeout, parameters))
            {
                return cmd.ExecuteScalar();
            }
        }

        /*
         * Asyncronous operations 
         */
        public static Task<object> ExecuteQueryScalarAsync(this DbConnection connection, string sql, params DbParameter[] parameters)
        {
            return connection.ExecuteQueryScalarAsync(sql, null, null, parameters);
        }

        public static Task<object> ExecuteQueryScalarAsync(this DbConnection connection, string sql, DbTransaction transaction, params DbParameter[] parameters)
        {
            return connection.ExecuteQueryScalarAsync(sql, transaction, null, parameters);
        }

        public static Task<object> ExecuteQueryScalarAsync(this DbConnection connection, string sql, DbTransaction transaction, int? timeout, params DbParameter[] parameters)
        {
            using (var cmd = connection.CreateCommand(CommandType.Text, sql, transaction, timeout, parameters))
            {
                return cmd.ExecuteScalarAsync();
            }
        }

        /*
         * Asyncronous operations with token
         */
        public static Task<object> ExecuteQueryScalarAsync(this DbConnection connection, string sql, CancellationToken token, params DbParameter[] parameters)
        {
            return connection.ExecuteQueryScalarAsync(sql, null, null, token, parameters);
        }

        public static Task<object> ExecuteQueryScalarAsync(this DbConnection connection, string sql, DbTransaction transaction, CancellationToken token, params DbParameter[] parameters)
        {
            return connection.ExecuteQueryScalarAsync(sql, transaction, null, token, parameters);
        }

        public static Task<object> ExecuteQueryScalarAsync(this DbConnection connection, string sql, DbTransaction transaction, int? timeout, CancellationToken token, params DbParameter[] parameters)
        {
            using (var cmd = connection.CreateCommand(CommandType.Text, sql, transaction, timeout, parameters))
            {
                return cmd.ExecuteScalarAsync(token);
            }
        }
    }
}
