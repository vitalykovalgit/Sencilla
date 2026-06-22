namespace Sencilla.Repository.EntityFramework;

/// <summary>
/// Repository for append-only valid-time entities (<see cref="IEntityAppendOnlyTrack"/>) where both Create
/// and Update <b>append</b>: the current open version for the entity's <see cref="BusinessKeyAttribute"/> is
/// closed (its <c>ActiveTo</c> stamped to now — the one mutation the append-only interceptor permits) and a
/// fresh open version is inserted. Nothing is overwritten; "update" is a supersede that returns the new
/// version (with a new identity). Lets API clients use create or update interchangeably.
///
/// The supersede runs as close-then-insert in a single transaction, so a "one open row per key" unique
/// index (recommended on the table) never sees two open rows at once, and a concurrent collision is retried.
/// </summary>
public class AppendOnlyTrackRepository<TEntity, TContext, TKey>
    : CreateRepository<TEntity, TContext, TKey>, IUpdateRepository<TEntity, TKey>
    where TEntity : class, IEntity<TKey>, IEntityAppendOnlyTrack, new()
    where TContext : DbContext
{
    const int MaxAttempts = 3;

    public AppendOnlyTrackRepository(RepositoryDependency dependency, TContext context) : base(dependency, context) { }

    public override async Task<IEnumerable<TEntity>> Create(IEnumerable<TEntity> entities, CancellationToken token = default)
    {
        var list = entities as IList<TEntity> ?? entities.ToList();

        // Only retry when we own the transaction; inside an ambient transaction a failed save dooms it.
        var canRetry = DbContext.Database.CurrentTransaction == null;

        for (var attempt = 1; ; attempt++)
        {
            try
            {
                return await Supersede(list, token).ConfigureAwait(false);
            }
            catch (DbUpdateException) when (canRetry && attempt < MaxAttempts)
            {
                // A concurrent supersede won the race (e.g. tripped the open-row unique index).
                // Re-read and retry; the close step will now see the other writer's open row.
                DbContext.ChangeTracker.Clear();
            }
        }
    }

    async Task<IEnumerable<TEntity>> Supersede(IList<TEntity> list, CancellationToken token)
    {
        var now = DateTime.UtcNow;
        var ownTx = DbContext.Database.CurrentTransaction == null;
        var tx = ownTx ? await DbContext.Database.BeginTransactionAsync(token).ConfigureAwait(false) : null;

        try
        {
            // Phase 1: close the current open version(s) for each key and persist — before any insert, so a
            // "one open row per key" unique index never sees two open rows at once.
            foreach (var e in list)
            {
                var open = await DbContext.Set<TEntity>()
                    .Where(BusinessKeys.Match(e))
                    .Where(x => x.ActiveTo == null)
                    .ToListAsync(token)
                    .ConfigureAwait(false);

                foreach (var prior in open)
                    prior.ActiveTo = now;
            }

            await Save(token).ConfigureAwait(false);

            // Phase 2: insert the new open versions (new identity, fresh open interval).
            foreach (var e in list)
            {
                e.Id = default!;
                e.ActiveFrom = now;
                e.ActiveTo = null;
            }

            // base.Create stamps CreatedDate, adds the rows, saves and publishes the create events.
            var result = await base.Create(list, token).ConfigureAwait(false);

            if (tx != null)
                await tx.CommitAsync(token).ConfigureAwait(false);

            return result;
        }
        catch
        {
            if (tx != null)
                await tx.RollbackAsync(token).ConfigureAwait(false);
            throw;
        }
        finally
        {
            if (tx != null)
                await tx.DisposeAsync().ConfigureAwait(false);
        }
    }

    // Update == append a new version (supersede); funnels through the overridden Create.
    public async Task<TEntity?> Update(TEntity entity, CancellationToken token = default)
        => (await Create([entity], token)).FirstOrDefault();

    public Task<IEnumerable<TEntity>> Update(params TEntity[] entities) => Create(entities, CancellationToken.None);

    public Task<IEnumerable<TEntity>> Update(IEnumerable<TEntity> entities, CancellationToken token = default) => Create(entities, token);

    public void Detach(IEnumerable<TEntity> entities)
    {
        foreach (var entity in entities)
            if (entity != null)
                DbContext.Entry(entity).State = EntityState.Detached;
    }

    public void ClearChangeTracker() => DbContext.ChangeTracker.Clear();

    public Task<int> JsonMergeAsync<TValue>(TKey id, Expression<Func<TEntity, IDictionary<string, TValue>?>> property, string key, TValue value, CancellationToken token = default)
        => DbContext.JsonMergeAsync(id, property, key, value, token);
}
