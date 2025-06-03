using MySql.Data;
using MySql.Data.MySqlClient;
using Sencilla.Infrastructure.SqlMapper.Provider;
using System.Data.Common;

namespace Sencilla.Mapper.Sql.DbProvider.MySql
{
    class MySqlDbProvider : IDbProvider
    {
        /// <summary>
        /// 
        /// </summary>
        public DbProviderType DbType => DbProviderType.MySql;

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public DbConnection GetDbConnection(string connectionString) 
        {
            return new MySqlConnection(connectionString);
        }

        /// <summary>
        /// 
        /// </summary>
        public IDbProviderParam Param { get; } = new MySqlDbProviderParam();
    }
}
