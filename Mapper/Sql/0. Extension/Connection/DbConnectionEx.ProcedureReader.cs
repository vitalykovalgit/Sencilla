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
         * No Async await 
         */
        public static DbDataReader ExecuteProcedureReader(this DbConnection connection, string spName, params DbParameter[] parameters)
        {
            return connection.ExecuteProcedureReader(spName, null, null, parameters);
        }

        public static DbDataReader ExecuteProcedureReader(this DbConnection connection, string spName, DbTransaction transaction, params DbParameter[] parameters)
        {
            return connection.ExecuteProcedureReader(spName, transaction, null, parameters);
        }

        public static DbDataReader ExecuteProcedureReader(this DbConnection connection, string spName, DbTransaction transaction, int? timeout, params DbParameter[] parameters)
        {
            using (var cmd = connection.CreateCommand(CommandType.StoredProcedure, spName, transaction, timeout, parameters))
            {
                return cmd.ExecuteReader();
            }
        }

        /*
         * No cancellation token 
         */
        public static Task<DbDataReader> ExecuteProcedureReaderAsync(this DbConnection connection, string spName, params DbParameter[] parameters)
        {
            return connection.ExecuteProcedureReaderAsync(spName, null, (int?)null, parameters);
        }

        public static Task<DbDataReader> ExecuteProcedureReaderAsync(this DbConnection connection, string spName, DbTransaction transaction, params DbParameter[] parameters)
        {
            return connection.ExecuteProcedureReaderAsync(spName, transaction, (int?)null, parameters);
        }

        public static Task<DbDataReader> ExecuteProcedureReaderAsync(this DbConnection connection, string spName, DbTransaction transaction, int? timeout, params DbParameter[] parameters)
        {
            using (var cmd = connection.CreateCommand(CommandType.StoredProcedure, spName, transaction, timeout, parameters))
            {
                return cmd.ExecuteReaderAsync();
            }
        }

        /*
         * Using cancelation token
         */
        public static Task<DbDataReader> ExecuteProcedureReaderAsync(this DbConnection connection, string spName, CancellationToken? token, params DbParameter[] parameters)
        {
            return connection.ExecuteProcedureReaderAsync(spName, null, null, token, parameters);
        }

        public static Task<DbDataReader> ExecuteProcedureReaderAsync(this DbConnection connection, string spName, DbTransaction transaction, CancellationToken? token, params DbParameter[] parameters)
        {
            return connection.ExecuteProcedureReaderAsync(spName, transaction, null, token, parameters);
        }

        public static Task<DbDataReader> ExecuteProcedureReaderAsync(this DbConnection connection, string spName, DbTransaction transaction, int? timeout, CancellationToken? token, params DbParameter[] parameters)
        {
            using (var cmd = connection.CreateCommand(CommandType.StoredProcedure, spName, transaction, timeout, parameters))
            {
                return cmd.ExecuteReaderAsync(token ?? CancellationToken.None);
            }
        }
    }
}
