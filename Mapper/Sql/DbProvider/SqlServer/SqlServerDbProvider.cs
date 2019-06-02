using Sencilla.Infrastructure.SqlMapper.Provider;
using System.Data.Common;
using System.Data.SqlClient;

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
    }
}
