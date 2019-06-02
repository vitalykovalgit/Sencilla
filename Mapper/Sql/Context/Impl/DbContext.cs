using Sencilla.Infrastructure.SqlMapper.Context;
using Sencilla.Infrastructure.SqlMapper.Contract;
using Sencilla.Infrastructure.SqlMapper.DbProvider.SqlServer;
using Sencilla.Infrastructure.SqlMapper.Impl.Set;
using Sencilla.Infrastructure.SqlMapper.Provider;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Concurrent;
using System.Configuration;
using System.Data.Common;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Sencilla.Infrastructure.SqlMapper.Impl
{
    public class DbContext : IDisposable
    {
        /// <summary>
        /// 
        /// </summary>
        readonly ConcurrentDictionary<string, object> SetsCache = new ConcurrentDictionary<string, object>();

        /// <summary>
        /// 
        /// </summary>
        public string ConnectionString { get; }
        
        /// <summary>
        /// 
        /// </summary>
        public IDbConnectionPolicy ConnectionPolicy { get; protected set; }

        /// <summary>
        /// Create parameters 
        /// </summary>
        public IDbProvider DbProvider { get; protected set; }

        /// <summary>
        /// Initialize DB Context
        /// </summary>
        /// <param name="connectionName"> name of connection string in DB </param>
        /// <param name="useTransaction"> create transaction if true </param>
        public DbContext(string connectionName, bool useTransaction = true)
        {
            Init();
            ConnectionString = ConfigurationManager.ConnectionStrings[connectionName]?.ConnectionString;
            ConnectionPolicy = new ConnectionInternalPolicy(null/*TODO: use config*/, ConnectionString, useTransaction);
        }

        public DbContext(IConfiguration config, string connectionName, bool useTransaction = true)
        {
            Init();

            //var config1 = new ConfigurationBuilder().AddJsonFile("AppSettings.json").Build();
            ConnectionString = config.GetConnectionString(connectionName);
            var azureAuthConfig = config.GetSection(nameof(AzureAuthenticationConfig)).Get<AzureAuthenticationConfig>();

            ConnectionPolicy = new ConnectionInternalPolicy(azureAuthConfig, ConnectionString, useTransaction);
        }

        public DbContext(IConfiguration config, bool useTransaction = true)
        {
            Init();
            ConnectionString = config.GetSection("ConnectionStrings").GetChildren().First().Value;

            var azureAuthConfig = config.GetSection(nameof(AzureAuthenticationConfig)).Get<AzureAuthenticationConfig>(); 
            ConnectionPolicy = new ConnectionInternalPolicy(azureAuthConfig, ConnectionString, useTransaction);
        }

        public DbContext(DbConnection connection, bool useTransaction = true)
        {
            Init();
            ConnectionPolicy = new ConnectionInternalPolicy(null, connection, useTransaction);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="connection"> Sql Connection to work with  </param>
        /// <param name="ownConnection"> If true context take care of connection </param>
        /// <param name="transaction"> Transaction </param>
        public DbContext(DbConnection connection, bool ownConnection = false, DbTransaction transaction = null, bool ownTransaction = false)
        {
            Init();
            SetConnection(connection, ownConnection, transaction, ownTransaction);
        }

        private void Init()
        {
            DbProvider = new SqlServerDbProvider();
        }

        internal void SetConnection(DbConnection connection, bool ownConnection = false, DbTransaction transaction = null, bool ownTransaction = false)
        {
            ConnectionPolicy?.Dispose(); // Dispose current connection 
            ConnectionPolicy = new ConnectionForeignPolicy(connection, ownConnection, transaction, ownTransaction);
        }

        public DbConnection Connection => ConnectionPolicy?.Connection;

        public DbTransaction Transaction => ConnectionPolicy?.Transaction;

        public DbConnection Connect()
        {
            return ConnectionPolicy?.Connect();
        }

        public void Disconnect()
        {
            ConnectionPolicy?.Disconnect();
        }

        public void Commit()
        {
            ConnectionPolicy?.Commit();
        }

        public void Rollback()
        {
            ConnectionPolicy?.Rollback();
        }

        public void Dispose()
        {
            ConnectionPolicy?.Dispose();
        }


        /// <summary>
        /// Represents table in database
        /// </summary>
        public ISet<TEntity, TKey> Set<TEntity, TKey>() where TEntity : class, new()
        {
            var key = typeof(TEntity).FullName;
            try
            {
                return (ISet<TEntity, TKey>)SetsCache[key];
            }
            catch (Exception)
            {
                // TODO: Think about creating default set 
                //return new DbSet<TEntity, TKey>(this);
                throw new InvalidOperationException($"Please initialize ISet in DbContext for entity {key}");
            }
        }

        /// <summary>
        /// Represents table in database
        /// </summary>
        public ISet<TEntity, int> Set<TEntity>() where TEntity : class, new()
        {
            return Set<TEntity, int>();
        }

        /// <summary>
        /// Represents table in which primary key is GUID 
        /// </summary>
        public ISet<TEntity, Guid> SetGuid<TEntity>() where TEntity : class, new()
        {
            return Set<TEntity, Guid>();
        }

        #region Sets Implementation 

        protected ISet<TEntity, TKey> SpSet<TEntity, TKey>() where TEntity : class, new()
        {
            var set = new SpSet<TEntity, TKey>(this);
            SetsCache[typeof(TEntity).FullName] = set;
            return set;
        }

        protected ISet<TEntity, TKey> QuerySet<TEntity, TKey>() where TEntity : class, new()
        {
            var set = new QuerySet<TEntity, TKey>(this);
            SetsCache[typeof(TEntity).FullName] = set;
            return set;
        }

        #endregion

        #region Execute queries 

        public int ExecuteQuery(string sql, params DbParameter[] parameters)
        {
            return ConnectionPolicy?.Connection.ExecuteQuery(sql, ConnectionPolicy?.Transaction, parameters) ?? 0;
        }

        public object ExecuteQueryScalar(string sql, params DbParameter[] parameters)
        {
            return ConnectionPolicy?.Connection.ExecuteQueryScalar(sql, ConnectionPolicy?.Transaction, parameters);
        }

        public DbDataReader ExecuteQueryReader(string sql, params DbParameter[] parameters)
        {
            return ConnectionPolicy?.Connection.ExecuteQueryReader(sql, ConnectionPolicy?.Transaction, parameters);
        }

        public Task<int> ExecuteQueryAsync(string sql, CancellationToken token, params DbParameter[] parameters)
        {
            return ConnectionPolicy?.Connection.ExecuteQueryAsync(sql, ConnectionPolicy?.Transaction, token, parameters);
        }

        public Task<object> ExecuteQueryScalarAsync(string sql, CancellationToken token, params DbParameter[] parameters)
        {
            return ConnectionPolicy?.Connection.ExecuteQueryScalarAsync(sql, ConnectionPolicy?.Transaction, token, parameters);
        }

        public Task<DbDataReader> ExecuteQueryReaderAsync(string sql, CancellationToken token, params DbParameter[] parameters)
        {
            return ConnectionPolicy?.Connection.ExecuteQueryReaderAsync(sql, ConnectionPolicy?.Transaction, token, parameters);
        }

        #endregion

        #region Execute stored procedures 

        public int ExecuteProcedure(string name, params DbParameter[] parameters)
        {
            return ConnectionPolicy?.Connection.ExecuteProcedure(name, ConnectionPolicy?.Transaction, parameters) ?? 0;
        }

        public object ExecuteProcedureScalar(string name, params DbParameter[] parameters)
        {
            return ConnectionPolicy?.Connection.ExecuteProcedureScalar(name, ConnectionPolicy?.Transaction, parameters);
        }

        public DbDataReader ExecuteProcedureReader(string name, params DbParameter[] parameters)
        {
            return ConnectionPolicy?.Connection.ExecuteProcedureReader(name, ConnectionPolicy?.Transaction, parameters);
        }

        public Task<int> ExecuteProcedureAsync(string name, CancellationToken? token, params DbParameter[] parameters)
        {
            return ConnectionPolicy?.Connection.ExecuteProcedureAsync(name, ConnectionPolicy?.Transaction, token, parameters);
        }

        public Task<object> ExecuteProcedureScalarAsync(string name, CancellationToken? token, params DbParameter[] parameters)
        {
            return ConnectionPolicy?.Connection.ExecuteProcedureScalarAsync(name, ConnectionPolicy?.Transaction, token, parameters);
        }

        public Task<DbDataReader> ExecuteProcedureReaderAsync(string sql, CancellationToken? token, params DbParameter[] parameters)
        {
            return ConnectionPolicy?.Connection.ExecuteProcedureReaderAsync(sql, ConnectionPolicy?.Transaction, token, parameters);
        }

        #endregion
    }
}
