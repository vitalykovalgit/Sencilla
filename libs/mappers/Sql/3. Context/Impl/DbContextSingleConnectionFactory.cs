//using Sencilla.Infrastructure.SqlMapper.Contract;
//using Sencilla.Infrastructure.SqlMapper.Impl;
//using System.Data;
//using System.Data.Common;
//using System.Data.SqlClient;

//namespace Sencilla.Infrastructure.SqlMapper.Context
//{
//    /// <summary>
//    /// Implements unit of work 
//    /// </summary>
//    public class DbContextSingleConnectionFactory : IDbContextFactory
//    {
//        public DbConnection Connection;
//        public DbTransaction Transaction;

//        public DbContextSingleConnectionFactory(string connectionString)
//        {
//            Connection = new SqlConnection(connectionString);
//            Connection.Open();
//            Transaction = Connection.BeginTransaction(IsolationLevel.ReadUncommitted);
//        }

//        public TContext Context<TContext>() where TContext : DbContext, new()
//        {
//            var context = new TContext();
//            context.SetConnection(Connection, false, Transaction, false);
//            return context;
//        }

//        public void Commit()
//        {
//            Transaction?.Commit();
//        }

//        public void Dispose()
//        {
//            Connection?.Dispose();
//            Transaction?.Dispose();
//        }
//    }
//}
