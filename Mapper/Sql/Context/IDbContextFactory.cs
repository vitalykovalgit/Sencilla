using Sencilla.Infrastructure.SqlMapper.Impl;
using System;

namespace Sencilla.Infrastructure.SqlMapper.Contract
{
    public interface IDbContextFactory : IDisposable
    {
        /// <summary>
        /// Create new instance of the context 
        /// </summary>
        /// <typeparam name="TContext"> Context type </typeparam>
        /// <returns></returns>
        TContext Context<TContext>() where TContext : DbContext, new();
    }
}
