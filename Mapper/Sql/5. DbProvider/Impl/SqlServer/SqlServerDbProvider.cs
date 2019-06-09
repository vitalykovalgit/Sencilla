using System.Data.Common;
using System.Data.SqlClient;

using Sencilla.Infrastructure.SqlMapper.Provider;

namespace Sencilla.Infrastructure.SqlMapper.DbProvider.SqlServer
{
    /// <summary>
    /// 
    /// </summary>
    public class SqlServerDbProvider : IDbProvider
    {
        /// <summary>
        /// 
        /// </summary>
        public DbProviderType DbType => DbProviderType.SqlServer;

        /// <summary>
        /// 
        /// </summary>
        public IDbProviderParam Param { get; } = new SqlServerDbProviderParam();

        /// <summary>
        /// 
        /// </summary>
        /// <param name="connectionString"></param>
        /// <returns></returns>
        public DbConnection GetDbConnection(string connectionString)
        {
            return new SqlConnection(connectionString);
        }
    }
}
