using System.Threading;
using System.Threading.Tasks;

namespace System.Data.Common
{
    /// <summary>
    /// 
    /// </summary>
    public static partial class DbConnectionEx
    {
        public static int ExecuteProcedure(this DbConnection connection, string spName, params DbParameter[] parameters)
        {
            return connection.ExecuteProcedure(spName, null, null, parameters);
        }

        public static int ExecuteProcedure(this DbConnection connection, string spName, DbTransaction transaction, params DbParameter[] parameters)
        {
            return connection.ExecuteProcedure(spName, transaction, null, parameters);
        }

        public static int ExecuteProcedure(this DbConnection connection, string spName, DbTransaction transaction, int? timeout, params DbParameter[] parameters)
        {
            using (var cmd = connection.CreateCommand(CommandType.StoredProcedure, spName, transaction, timeout, parameters))
            {
                return cmd.ExecuteNonQuery();
            }
        }

        public static Task<int> ExecuteProcedureAsync(this DbConnection connection, string spName, params DbParameter[] parameters)
        {
            return connection.ExecuteProcedureAsync(spName, null, (int?)null, parameters);
        }

        public static Task<int> ExecuteProcedureAsync(this DbConnection connection, string spName, DbTransaction transaction, params DbParameter[] parameters)
        {
            return connection.ExecuteProcedureAsync(spName, transaction, (int?)null, parameters);
        }

        public static Task<int> ExecuteProcedureAsync(this DbConnection connection, string spName, DbTransaction transaction, int? timeout, params DbParameter[] parameters)
        {
            using (var cmd = connection.CreateCommand(CommandType.StoredProcedure, spName, transaction, timeout, parameters))
            {
                return cmd.ExecuteNonQueryAsync();
            }
        }

        public static Task<int> ExecuteProcedureAsync(this DbConnection connection, string spName, CancellationToken? token, params DbParameter[] parameters)
        {
            return connection.ExecuteProcedureAsync(spName, null, null, token, parameters);
        }

        public static Task<int> ExecuteProcedureAsync(this DbConnection connection, string spName, DbTransaction transaction, CancellationToken? token, params DbParameter[] parameters)
        {
            return connection.ExecuteProcedureAsync(spName, transaction, null, token, parameters);
        }

        public static Task<int> ExecuteProcedureAsync(this DbConnection connection, string spName, DbTransaction transaction, int? timeout, CancellationToken? token, params DbParameter[] parameters)
        {
            using (var cmd = connection.CreateCommand(CommandType.StoredProcedure, spName, transaction, timeout, parameters))
            {
                return cmd.ExecuteNonQueryAsync(token ?? CancellationToken.None);
            }
        }
    }
}
