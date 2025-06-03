namespace Sencilla.Repository.EntityFramework;

public class BaseRepository<TContext> : Resolveable, IBaseRepository where TContext : DbContext
{
    /// <summary>
    /// 
    /// </summary>
    protected TContext DbContext { get; }
    
    /// <summary>
    /// Reposiroty dependency
    /// </summary>
    protected RepositoryDependency D { get; }

    /// <summary>
    /// 
    /// </summary>
    protected BaseRepository(RepositoryDependency dependency, TContext context): base(dependency.Resolver)
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
