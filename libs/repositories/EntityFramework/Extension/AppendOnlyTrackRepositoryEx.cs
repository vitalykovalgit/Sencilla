namespace Sencilla.Repository.EntityFramework;

/// <summary>
/// Point-in-time reads for <see cref="IEntityAppendOnlyTrack"/> entities. "Active as of D" is a plain
/// range predicate over the stored [ActiveFrom, ActiveTo) interval — sargable, index-friendly, and fully
/// composable as an <see cref="IQueryable{T}"/>. With supersede-on-write keeping intervals non-overlapping,
/// this yields exactly one row per business key.
/// </summary>
public static class AppendOnlyTrackRepositoryEx
{
    /// <summary>Narrow a queryable to the rows active at <paramref name="asOf"/> (UTC).</summary>
    public static IQueryable<T> ActiveAsOf<T>(this IQueryable<T> source, DateTime asOf) where T : class, IEntityAppendOnlyTrack
        => source.Where(e => (e.ActiveFrom == null || e.ActiveFrom <= asOf) && (e.ActiveTo == null || e.ActiveTo > asOf));

    /// <summary>The rows active right now (UTC).</summary>
    public static Task<IReadOnlyList<T>> Current<T, TKey>(this IReadRepository<T, TKey> repo, CancellationToken token = default)
        where T : class, IEntityAppendOnlyTrack, IEntity<TKey>
        => repo.AsOf(DateTime.UtcNow, token);

    /// <summary>The rows active as of <paramref name="asOf"/> (UTC).</summary>
    public static async Task<IReadOnlyList<T>> AsOf<T, TKey>(this IReadRepository<T, TKey> repo, DateTime asOf, CancellationToken token = default)
        where T : class, IEntityAppendOnlyTrack, IEntity<TKey>
        => await repo.Query.ActiveAsOf(asOf).AsNoTracking().ToListAsync(token).ConfigureAwait(false);
}
