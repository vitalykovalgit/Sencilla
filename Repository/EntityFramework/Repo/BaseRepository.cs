using Microsoft.EntityFrameworkCore;
using Sencilla.Core;

namespace Sencilla.Repository.EntityFramework
{
    public class BaseRepository<TContext> : IDisposable, IBaseRepository where TContext : DbContext
    {
        private TContext? DbContext = null;
        /// <summary>
        /// Container to resolve context, please use resolve instead 
        /// </summary>
        protected IResolver Resolver { get; set; }

        /// <summary>
        /// Context implementation
        /// </summary>
        protected TContext? Context => DbContext ??= R<TContext>();

        protected IEnumerable<IReadConstraint>? Constraints => R<IEnumerable<IReadConstraint>>();

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
        public T? R<T>()
        {
            return Resolver.Resolve<T>();
        }

        /// <summary>
        /// 
        /// </summary>
        public async Task<int> Save()
        {
            return DbContext == null ? 0 : await DbContext.SaveChangesAsync();
        }

        public void Dispose()
        {
            DbContext?.Dispose();
        }
    }
}
