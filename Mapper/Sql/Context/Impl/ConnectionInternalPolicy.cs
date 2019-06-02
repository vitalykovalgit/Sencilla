using System.Data;
using System.Data.Common;
using System.Data.SqlClient;
using Sencilla.Infrastructure.SqlMapper.Impl;
using Microsoft.Azure.Services.AppAuthentication;

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
        protected DbConnection SqlConnection { get; set; }

        /// <summary>
        /// 
        /// </summary>
        protected AzureAuthenticationConfig AzureAuthConfig { get; set; }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="connectionString"></param>
        /// <param name="useTransaction"></param>
        public ConnectionInternalPolicy(AzureAuthenticationConfig azureConfig, string connectionString, bool useTransaction)
        {
            AzureAuthConfig = azureConfig;
            UseTransaction = useTransaction;
            ConnectionString = connectionString;
        }

        public ConnectionInternalPolicy(AzureAuthenticationConfig azureConfig, DbConnection connection, bool useTransaction)
        {
            AzureAuthConfig = azureConfig;
            UseTransaction = useTransaction;
            ConnectionString = connection?.ConnectionString;
            SqlConnection = connection;
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
            if (SqlConnection == null)
            {
                SqlConnection = new SqlConnection(ConnectionString);

                if (AzureAuthConfig?.UseAccessToken ?? false)
                {
                    var token = new AzureServiceTokenProvider().GetAccessTokenAsync(AzureAuthConfig?.AccessTokenProvider).Result;
                    ((SqlConnection)SqlConnection).AccessToken = token;
                }
            }

            if (SqlConnection.State == ConnectionState.Closed)
            {
                SqlConnection.Open();

                if (UseTransaction)
                {
                    // TODO: Think about what to do with current transaction, try to rollback for now 
                    Rollback();
                    Transaction = SqlConnection.BeginTransaction(IsolationLevel.Serializable);
                }
            }

            return SqlConnection;
        }

        /// <summary>
        /// 
        /// </summary>
        public void Disconnect()
        {
            try
            {
                SqlConnection?.Close();
            }
            finally
            {
                SqlConnection = null;
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
