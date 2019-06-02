using System;
using System.Data.Common;

namespace Sencilla.Infrastructure.SqlMapper.Impl
{
    /// <inheritdoc />
    /// <summary>
    /// Represents connection policy of DbContext 
    /// </summary>
    public interface IDbConnectionPolicy : IDisposable
    {
        DbTransaction Transaction { get; }

        DbConnection Connection { get; }

        DbConnection Connect();

        void Disconnect();

        void Commit();

        void Rollback();
        
    }
}