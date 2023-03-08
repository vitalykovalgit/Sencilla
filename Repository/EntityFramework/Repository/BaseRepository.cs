
namespace Sencilla.Repository.EntityFramework
{
    public class BaseRepository<TContext> : Resolveable, IBaseRepository where TContext : DbContext
    {
        /// <summary>
        /// 
        /// </summary>
        public TContext DbContext { get; }

        /// <summary>
        /// Resolve here allows us to avoid circular dependency 
        /// </summary>
        //public IEnumerable<IReadConstraint>? Constraints => R<IEnumerable<IReadConstraint>>();

        /// <summary>
        /// Reposiroty dependency
        /// </summary>
        public RepositoryDependency D { get; }

        /// <summary>
        /// 
        /// </summary>
        public BaseRepository(RepositoryDependency dependency, TContext context): base(dependency.Resolver)
        {
            D = dependency;
            DbContext = context;
        }

        /// <summary>
        /// 
        /// </summary>
        public async Task<int> Save(CancellationToken token = default)
        {
            return await DbContext.SaveChangesAsync(token).ConfigureAwait(false);
        }
    }
}
