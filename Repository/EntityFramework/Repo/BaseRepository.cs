using Microsoft.EntityFrameworkCore;
using Sencilla.Core;

namespace Sencilla.Repository.EntityFramework
{
    public class BaseRepository<TContext> : Resolveable, IBaseRepository where TContext : DbContext
    {
        /// <summary>
        /// 
        /// </summary>
        protected TContext DbContext { get; }
        
        /// <summary>
        /// Resolve here allows us to avoid circular dependency 
        /// </summary>
        protected IEnumerable<IReadConstraint>? Constraints => R<IEnumerable<IReadConstraint>>();

        /// <summary>
        /// 
        /// </summary>
        public BaseRepository(IResolver resolver, TContext context): base(resolver)
        {
            DbContext = context;
        }

        /// <summary>
        /// 
        /// </summary>
        public async Task<int> Save(CancellationToken token = default)
        {
            return await DbContext.SaveChangesAsync(token);
        }
    }
}
