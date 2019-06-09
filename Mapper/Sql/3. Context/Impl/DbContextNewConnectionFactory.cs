using Sencilla.Infrastructure.SqlMapper.Contract;
using Sencilla.Infrastructure.SqlMapper.Impl;

namespace Sencilla.Infrastructure.SqlMapper.Context
{
    /// <summary>
    /// 
    /// </summary>
    public class DbContextNewConnectionFactory : IDbContextFactory
    {
        public TContext Context<TContext>() where TContext : DbContext, new()
        {
            return new TContext();
        }

        public void Dispose()
        {
        }
    }
}