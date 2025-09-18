
using Microsoft.Extensions.DependencyInjection;
using Sencilla.Core;
using Sencilla.Infrastructure.SqlMapper.Impl;
using System.Threading;
using System.Threading.Tasks;


namespace Sencilla.Impl.Repository.SqlMapper
{
    public class BaseRepository<TContext>(IServiceProvider resolver) : IBaseRepository
           where TContext : DbContext
    {

        /// <summary>
        /// Context implementation
        /// </summary>
        protected TContext ContextImpl { get; set; }
        
        /// <summary>
        /// 
        /// </summary>
        public async Task<int> Save(CancellationToken token = default)
        {
            ContextImpl?.Commit();
            return 0;
        }

        /// <summary>
        /// Encapsulates work with Unity container 
        /// </summary>
        /// <typeparam name="T"> Type for which instance will be created </typeparam>
        /// <returns></returns>
        public T Resolve<T>()
        {
            return resolver.GetService<T>();
        }
    }
}
