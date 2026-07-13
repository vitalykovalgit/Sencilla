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

    /// <summary>
    /// All-or-nothing enforcement (deny the batch, not the row): when an event
    /// handler narrowed the database query — permission constraints compose into
    /// it with Where() — every existing target row must still match, otherwise the
    /// whole operation is forbidden. Rows that never existed do not count as denied.
    /// No handler touched the query → no extra round trips.
    /// </summary>
    protected async Task ThrowIfNarrowed<T>(IQueryable<T>? narrowed, IQueryable<T> original, CancellationToken token) where T : class
    {
        if (narrowed == null || ReferenceEquals(narrowed, original))
            return;

        var allowed = await narrowed.CountAsync(token).ConfigureAwait(false);
        var existing = await original.CountAsync(token).ConfigureAwait(false);
        if (allowed < existing)
            throw new ForbiddenException("Operation is not permitted for some of the target entities");
    }

    /// <summary>
    /// Runs a write operation inside an ambient transaction so the -ing event,
    /// the save and the -ed handlers commit or roll back as one unit.
    /// Reuses the caller's transaction when one is already open (the caller owns
    /// commit/rollback then). Skipped on non-relational providers (e.g. InMemory),
    /// which do not support transactions.
    /// </summary>
    protected async Task<T> InTransaction<T>(Func<Task<T>> operation, CancellationToken token = default)
    {
        if (!DbContext.Database.IsRelational() || DbContext.Database.CurrentTransaction != null)
            return await operation().ConfigureAwait(false);

        await using var transaction = await DbContext.Database.BeginTransactionAsync(token).ConfigureAwait(false);
        var result = await operation().ConfigureAwait(false);
        await transaction.CommitAsync(token).ConfigureAwait(false);
        return result;
    }
}
