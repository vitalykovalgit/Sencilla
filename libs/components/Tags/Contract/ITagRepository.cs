
namespace Sencilla.Component.Tags;

/// <summary>
/// Reads and writes one taggable entity's tags, hiding which of the three storage strategies
/// (<see cref="IEntityTaggableInline"/> / <see cref="IEntityTaggableLinked"/> /
/// <see cref="IEntityTaggableShared"/>) is in play. One implementation per strategy; the filter pipeline and the
/// generic tag endpoints are written once against this contract.
///
/// <para>Registered per taggable entity by <c>AddSencillaTags()</c>. All names crossing this boundary are
/// already normalised by <see cref="TagName"/>.</para>
/// </summary>
/// <typeparam name="TEntity">The tagged entity.</typeparam>
/// <typeparam name="TKey">Its primary key type.</typeparam>
public interface ITagRepository<TEntity, TKey> where TEntity : class, IEntity<TKey>
{
    /// <summary>One entity's tags, ordinally sorted; empty when untagged.</summary>
    Task<IReadOnlyList<string>> Get(TKey id, CancellationToken token = default);

    /// <summary>
    /// Replaces the entity's whole tag set (the only write semantic — see the tags grill, decision 4). Empty
    /// clears. Always moves the parent's <c>UpdatedDate</c>/audit trail so cached copies invalidate, and is
    /// atomic for the multi-row repositories.
    /// </summary>
    Task Set(TKey id, IEnumerable<string> tags, CancellationToken token = default);

    /// <summary>Removes the named tags, ignoring ones the entity does not carry.</summary>
    Task Remove(TKey id, IEnumerable<string> tags, CancellationToken token = default);

    /// <summary>
    /// Fills <c>tags</c> on already-materialised entities, in ONE query for the whole page — the
    /// <c>EntityReadEvent</c> handler's body. A no-op for the inline repository, whose column is already loaded.
    /// </summary>
    Task Hydrate(IList<TEntity> entities, CancellationToken token = default);

    /// <summary>
    /// Narrows a query to rows carrying ANY of <paramref name="tags"/> (the <c>?tag=a&amp;tag=b</c> semantic).
    /// An empty array returns the query untouched.
    ///
    /// <para>Async because the side-table repositories resolve matching ids in a separate query and narrow with
    /// <c>ids.Contains(e.Id)</c> — an IN list, which translates for a generic key (as
    /// <c>ReadRepository.GetByIds</c> already relies on) where a correlated subquery over a generic key does
    /// not. ponytail: the IN list is the ceiling — swap in a correlated EXISTS (SecureWithFilter-style
    /// expression building) if a taggable entity ever matches tens of thousands of rows.</para>
    /// </summary>
    Task<IQueryable<TEntity>> FilterAny(IQueryable<TEntity> query, string[] tags, CancellationToken token = default);

    /// <summary>Every tag in use on this entity, ordinally sorted — the admin autocomplete source.</summary>
    Task<IReadOnlyList<string>> Distinct(CancellationToken token = default);
}
