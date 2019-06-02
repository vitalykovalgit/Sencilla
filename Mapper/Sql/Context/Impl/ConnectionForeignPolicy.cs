using Sencilla.Infrastructure.SqlMapper.Impl;
using System.Data.Common;

namespace Sencilla.Infrastructure.SqlMapper.Context
{
    public class ConnectionForeignPolicy : IDbConnectionPolicy
    {
        protected bool OwnConnection;
        protected bool OwnTransaction;

        public ConnectionForeignPolicy(DbConnection connection, bool ownConnection, DbTransaction transaction, bool ownTransaction)
        {
            Connection = connection;
            OwnConnection = ownConnection;

            Transaction = transaction;
            OwnTransaction = ownTransaction;
        }

        public DbTransaction Transaction { get; protected set; }

        public DbConnection Connection { get; protected set; }

        public DbConnection Connect()
        {
            // Do nothing
            return Connection;
        }

        public void Disconnect()
        {
            if (OwnConnection)
            {
                try
                {
                    Connection?.Close();
                }
                finally
                {
                    Connection = null;
                }
            }
        }

        public void Commit()
        {
            // TODO: Think if we need to do commit or rollback at all 
            if (OwnTransaction)
            {
                Transaction?.Commit();
                Transaction = null;
            }
        }

        public void Rollback()
        {
            if (OwnTransaction)
            {
                Transaction?.Rollback();
                Transaction = null;
            }
        }

        public void Dispose()
        {
            Rollback();
            Disconnect();
        }
    }
}
