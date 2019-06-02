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
        public static DbDataReader ExecuteQueryReader(this DbConnection connection, string sql, params DbParameter[] parameters)
        {
            return connection.ExecuteQueryReader(sql, null, null, parameters);
        }

        public static DbDataReader ExecuteQueryReader(this DbConnection connection, string sql, DbTransaction transaction, params DbParameter[] parameters)
        {
            return connection.ExecuteQueryReader(sql, transaction, null, parameters);
        }
        
        public static DbDataReader ExecuteQueryReader(this DbConnection connection, string sql, DbTransaction transaction, int? timeout, params DbParameter[] parameters)
        {
            using (var cmd = connection.CreateCommand(CommandType.Text, sql, transaction, timeout, parameters))
            {
                return cmd.ExecuteReader();
            }
        }

        /*
         *  Asyncronous operations 
         */
        public static Task<DbDataReader> ExecuteQueryReaderAsync(this DbConnection connection, string sql, params DbParameter[] parameters)
        {
            return connection.ExecuteQueryReaderAsync(sql, null, null, parameters);
        }

        public static Task<DbDataReader> ExecuteQueryReaderAsync(this DbConnection connection, string sql, DbTransaction transaction, params DbParameter[] parameters)
        {
            return connection.ExecuteQueryReaderAsync(sql, transaction, null, parameters);
        }

        public static Task<DbDataReader> ExecuteQueryReaderAsync(this DbConnection connection, string sql, DbTransaction transaction, int? timeout, params DbParameter[] parameters)
        {
            using (var cmd = connection.CreateCommand(CommandType.Text, sql, transaction, timeout, parameters))
            {
                return cmd.ExecuteReaderAsync();
            }
        }

        /*
         *  Asyncronous operations with cancellation token
         */
        public static Task<DbDataReader> ExecuteQueryReaderAsync(this DbConnection connection, string sql, CancellationToken token, params DbParameter[] parameters)
        {
            return connection.ExecuteQueryReaderAsync(sql, null, null, token, parameters);
        }

        public static Task<DbDataReader> ExecuteQueryReaderAsync(this DbConnection connection, string sql, DbTransaction transaction, CancellationToken token, params DbParameter[] parameters)
        {
            return connection.ExecuteQueryReaderAsync(sql, transaction, null, token, parameters);
        }

        public static Task<DbDataReader> ExecuteQueryReaderAsync(this DbConnection connection, string sql, DbTransaction transaction, int? timeout, CancellationToken token, params DbParameter[] parameters)
        {
            using (var cmd = connection.CreateCommand(CommandType.Text, sql, transaction, timeout, parameters))
            {
                return cmd.ExecuteReaderAsync(token);
            }
        }
    }
}
