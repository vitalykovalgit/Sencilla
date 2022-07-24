using Microsoft.EntityFrameworkCore;
using Sencilla.Core;

namespace Sencilla.Repository.EntityFramework
{
    public class BaseRepository<TContext> : IBaseRepository
           where TContext : DbContext
    {
        /// <summary>
        /// Container to resolve context, please use resolve instead 
        /// </summary>
        protected IResolver Resolver { get; set; }

        /// <summary>
        /// Context implementation
        /// </summary>
        protected TContext? Context { get; set; }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="resolver"></param>
        public BaseRepository(IResolver resolver)
        {
            Resolver = resolver;
        }

        /// <summary>
        /// Encapsulates work with Unity container 
        /// </summary>
        /// <typeparam name="T"> Type for which instance will be created </typeparam>
        /// <returns></returns>
        public T R<T>()
        {
            return Resolver.Resolve<T>();
        }

        /// <summary>
        /// 
        /// </summary>
        public async Task<int> Save()
        {
            return Context != null ? await Context.SaveChangesAsync() : 0;
        }
    }
}
