namespace Sencilla.Repository.EntityFramework;

/// <summary>
/// Repository for append-only valid-time entities (<see cref="IEntityAppendOnlyTrack"/>) where both Create
/// and Update <b>append</b>: the current open version for the entity's <see cref="BusinessKeyAttribute"/> is
/// closed (its <c>ActiveTo</c> stamped to the new version's start — the one mutation the append-only
/// interceptor permits) and a fresh open version is inserted. Nothing is overwritten; "update" is a
/// supersede that returns the new version (with a new identity). Lets API clients use create or update
/// interchangeably.
///
/// A version may be <b>scheduled</b> by supplying a future <see cref="IEntityAppendOnlyTrack.ActiveFrom"/>:
/// the close/insert happens at that instant instead of now, so reads (range/as-of) auto-activate it when
/// the clock crosses it — no job needed. Scheduling is <b>append-to-the-end only</b> (the effective instant
/// must fall strictly after the current open version's start); out-of-order/mid-interval inserts (true
/// backdating) are rejected. A past <see cref="IEntityAppendOnlyTrack.ActiveFrom"/> is snapped to now.
///
/// The supersede runs as close-then-insert in a single transaction, so a "one open row per key" unique
/// index (recommended on the table) never sees two open rows at once, and a concurrent collision is retried.
/// </summary>
public class AppendOnlyTrackRepository<TEntity, TContext, TKey>
    : CreateRepository<TEntity, TContext, TKey>, IUpdateRepository<TEntity, TKey>, IAppendOnlyTrackRepository<TEntity, TKey>
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
        var cuts = new DateTime[list.Count];
        var ownTx = DbContext.Database.CurrentTransaction == null;
        var tx = ownTx ? await DbContext.Database.BeginTransactionAsync(token).ConfigureAwait(false) : null;

        try
        {
            // Phase 1: resolve each version's effective instant ("cut-point"), close the current open
            // version to it, and persist — before any insert, so a "one open row per key" unique index
            // never sees two open rows at once.
            for (var i = 0; i < list.Count; i++)
            {
                var e = list[i];

                // A future ActiveFrom schedules the change; null or a past value means "effective now".
                // Snapping a past value to now only ever moves the interval forward, so it cannot re-price
                // already-booked rows — we never backdate.
                var cut = e.ActiveFrom is DateTime from && from > now ? from : now;

                var open = await DbContext.Set<TEntity>()
                    .Where(BusinessKeys.Match(e))
                    .Where(x => x.ActiveTo == null)
                    .ToListAsync(token)
                    .ConfigureAwait(false);

                foreach (var prior in open)
                {
                    // Append-to-the-end only: the cut-point must fall strictly after the current open
                    // version's start. A cut at/before it would overlap or reorder the timeline — e.g. an
                    // immediate change while a later one is already scheduled, or an out-of-order schedule.
                    if (prior.ActiveFrom is DateTime priorFrom && cut <= priorFrom)
                        throw new BadRequestException(
                            $"Cannot supersede {typeof(TEntity).Name}: effective date {cut:o} is not after the " +
                            $"current version's start {priorFrom:o}. A later change is already scheduled — cancel " +
                            $"it first, or schedule after it.");

                    prior.ActiveTo = cut;
                }

                cuts[i] = cut;
            }

            await Save(token).ConfigureAwait(false);

            // Phase 2: insert the new open versions (new identity, open interval starting at the cut-point).
            for (var i = 0; i < list.Count; i++)
            {
                var e = list[i];
                e.Id = default!;
                e.ActiveFrom = cuts[i];
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

    /// <summary>
    /// Cancels the latest scheduled (not-yet-active) version for the entity's business key and reopens the
    /// version it superseded. LIFO: only the open future tail (<c>ActiveTo == null</c>, <c>ActiveFrom &gt;
    /// now</c>) is removable; the row it had closed is reopened (<c>ActiveTo</c> → null). Runs
    /// delete-then-reopen in one transaction so the open-row unique index never sees two open rows.
    /// </summary>
    public async Task CancelPending(TEntity key, CancellationToken token = default)
    {
        var now = DateTime.UtcNow;
        var ownTx = DbContext.Database.CurrentTransaction == null;
        var tx = ownTx ? await DbContext.Database.BeginTransactionAsync(token).ConfigureAwait(false) : null;

        try
        {
            var tail = await DbContext.Set<TEntity>()
                .Where(BusinessKeys.Match(key))
                .Where(x => x.ActiveTo == null)
                .SingleOrDefaultAsync(token)
                .ConfigureAwait(false);

            if (tail is null || tail.ActiveFrom is not DateTime tailFrom || tailFrom <= now)
                throw new BadRequestException(
                    $"No scheduled (future) {typeof(TEntity).Name} version to cancel for this key — the open " +
                    $"version is already active.");

            // The version this tail superseded closed exactly at the tail's start, if any.
            var prior = await DbContext.Set<TEntity>()
                .Where(BusinessKeys.Match(key))
                .Where(x => x.ActiveTo == tailFrom)
                .SingleOrDefaultAsync(token)
                .ConfigureAwait(false);

            // Phase 1: remove the scheduled tail (allowed: its whole interval is in the future).
            DbContext.Set<TEntity>().Remove(tail);
            await Save(token).ConfigureAwait(false);

            // Phase 2: reopen the version it had closed (allowed: that close-point is in the future).
            if (prior is not null)
            {
                prior.ActiveTo = null;
                await Save(token).ConfigureAwait(false);
            }

            if (tx != null)
                await tx.CommitAsync(token).ConfigureAwait(false);
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
