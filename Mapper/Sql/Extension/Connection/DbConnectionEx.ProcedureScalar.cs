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
        public static object ExecuteProcedureScalar(this DbConnection connection, string spName, params DbParameter[] parameters)
        {
            return connection.ExecuteProcedureScalar(spName, null, null, parameters);
        }

        public static object ExecuteProcedureScalar(this DbConnection connection, string spName, DbTransaction transaction, params DbParameter[] parameters)
        {
            return connection.ExecuteProcedureScalar(spName, transaction, null, parameters);
        }

        public static object ExecuteProcedureScalar(this DbConnection connection, string spName, DbTransaction transaction, int? timeout, params DbParameter[] parameters)
        {
            using (var cmd = connection.CreateCommand(CommandType.StoredProcedure, spName, transaction, timeout, parameters))
            {
                return cmd.ExecuteScalar();
            }
        }

        /*
         * Asynchronous no token
         */
        public static Task<object> ExecuteProcedureScalarAsync(this DbConnection connection, string spName, params DbParameter[] parameters)
        {
            return connection.ExecuteProcedureScalarAsync(spName, null, (int?)null, parameters);
        }

        public static Task<object> ExecuteProcedureScalarAsync(this DbConnection connection, string spName, DbTransaction transaction, params DbParameter[] parameters)
        {
            return connection.ExecuteProcedureScalarAsync(spName, transaction, (int?)null, parameters);
        }

        public static Task<object> ExecuteProcedureScalarAsync(this DbConnection connection, string spName, DbTransaction transaction, int? timeout, params DbParameter[] parameters)
        {
            using (var cmd = connection.CreateCommand(CommandType.StoredProcedure, spName, transaction, timeout, parameters))
            {
                return cmd.ExecuteScalarAsync();
            }
        }

        /*
         * Asynchronous using token
         */
        public static Task<object> ExecuteProcedureScalarAsync(this DbConnection connection, string spName, CancellationToken? token, params DbParameter[] parameters)
        {
            return connection.ExecuteProcedureScalarAsync(spName, null, null, token, parameters);
        }

        public static Task<object> ExecuteProcedureScalarAsync(this DbConnection connection, string spName, DbTransaction transaction, CancellationToken? token, params DbParameter[] parameters)
        {
            return connection.ExecuteProcedureScalarAsync(spName, transaction, null, token, parameters);
        }

        public static Task<object> ExecuteProcedureScalarAsync(this DbConnection connection, string spName, DbTransaction transaction, int? timeout, CancellationToken? token, params DbParameter[] parameters)
        {
            using (var cmd = connection.CreateCommand(CommandType.StoredProcedure, spName, transaction, timeout, parameters))
            {
                return cmd.ExecuteScalarAsync(token ?? CancellationToken.None);
            }
        }
    }
}
