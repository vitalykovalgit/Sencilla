using System.Data.Common;

namespace Sencilla.Infrastructure.SqlMapper.Provider
{
    /// <summary>
    /// 
    /// </summary>
    public interface IDbProvider
    {
        /// <summary>
        /// 
        /// </summary>
        DbProviderType DbType { get; }

        /// <summary>
        /// 
        /// </summary>
        IDbProviderParam Param { get; }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        DbConnection GetDbConnection(string connectionString);
    }
}
