using System.Data;
using System.Data.Common;
using System.Data.SqlClient;
using Sencilla.Infrastructure.SqlMapper.Impl;
using Sencilla.Infrastructure.SqlMapper.Provider;

namespace Sencilla.Infrastructure.SqlMapper.Context
{
    /// <inheritdoc />
    /// <summary>
    /// </summary>
    public class ConnectionInternalPolicy : IDbConnectionPolicy
    {
        /// <summary>
        /// 
        /// </summary>
        protected bool UseTransaction { get; }

        /// <summary>
        /// 
        /// </summary>
        protected string ConnectionString { get; }

        /// <summary>
        /// 
        /// </summary>
        protected DbConnection DbConnection { get; set; }

        /// <summary>
        /// 
        /// </summary>
        protected AzureAuthenticationConfig AzureAuthConfig { get; set; }

        protected IDbProvider DbProvider { get; set; }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="connectionString"></param>
        /// <param name="useTransaction"></param>
        public ConnectionInternalPolicy(AzureAuthenticationConfig azureConfig, IDbProvider provider, string connectionString, bool useTransaction)
        {
            DbProvider = provider;
            AzureAuthConfig = azureConfig;
            UseTransaction = useTransaction;
            ConnectionString = connectionString;
        }

        public ConnectionInternalPolicy(AzureAuthenticationConfig azureConfig, DbConnection connection, bool useTransaction)
        {
            AzureAuthConfig = azureConfig;
            UseTransaction = useTransaction;
            ConnectionString = connection?.ConnectionString;
            DbConnection = connection;
        }

        /// <summary>
        /// 
        /// </summary>
        public DbConnection Connection => Connect();

        /// <summary>
        /// 
        /// </summary>
        public DbTransaction Transaction { get; protected set; }

        /// <summary>
        /// Try to conect and returns connection 
        /// if connection is open it will do nothing 
        /// Throw exception in case of any problem
        /// </summary>
        public DbConnection Connect()
        {
            if (DbConnection == null)
            {
                DbConnection = DbProvider.GetDbConnection(ConnectionString);

                //if (AzureAuthConfig?.UseAccessToken ?? false)
                //{
                //    var token = new AzureServiceTokenProvider().GetAccessTokenAsync(AzureAuthConfig?.AccessTokenProvider).Result;
                //    ((SqlConnection)DbConnection).AccessToken = token;
                //}
            }

            if (DbConnection.State == ConnectionState.Closed)
            {
                DbConnection.Open();

                if (UseTransaction)
                {
                    // TODO: Think about what to do with current transaction, try to rollback for now 
                    Rollback();
                    Transaction = DbConnection.BeginTransaction(IsolationLevel.Serializable);
                }
            }

            return DbConnection;
        }

        /// <summary>
        /// 
        /// </summary>
        public void Disconnect()
        {
            try
            {
                DbConnection?.Close();
            }
            finally
            {
                DbConnection = null;
            }
        }
        
        public void Commit()
        {
            Transaction?.Commit();
            Transaction = null;
        }

        /// <summary>
        /// 
        /// </summary>
        public void Rollback()
        {
            try
            {
                Transaction?.Rollback();
            }
            finally
            {
                Transaction = null;
            }
        }

        public void Dispose()
        {
            try
            {
                Rollback();
                Disconnect();
            }
            catch
            {
                // Hide exceptions from dispose 
            }
        }
    }
}
