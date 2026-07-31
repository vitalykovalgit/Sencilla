namespace Sencilla.Component.Tags;

/// <summary>
/// What the three tag repositories share, on top of the framework's own <see cref="ReadRepository{TEntity,TContext,TKey}"/>
/// — which is where the DbContext, the resolver (<c>R&lt;T&gt;</c>), <c>Save</c> and the ambient-transaction
/// helper come from, so none of that is reinvented here.
///
/// <para>What IS here: the parent "touch" that makes a tag edit look like any other edit of the tagged row
/// (<c>UpdatedDate</c> moves, the audit log records it, row-keyed caches invalidate), removal expressed as
/// set-minus over <see cref="Set"/>, and the assignment of hydrated tags onto entities.</para>
/// </summary>
public abstract class TagReadRepository<TEntity, TKey>(RepositoryDependency dependency, DynamicDbContext context)
    : ReadRepository<TEntity, DynamicDbContext, TKey>(dependency, context), ITagRepository<TEntity, TKey>
    where TEntity : class, IEntity<TKey>, IEntityTaggable, new()
{
    /// <summary>The tagged entity's type name — the discriminator the shared repository writes.</summary>
    protected static readonly string EntityName = typeof(TEntity).Name;

    public abstract Task<IReadOnlyList<string>> Get(TKey id, CancellationToken token = default);
    public abstract Task Set(TKey id, IEnumerable<string> tags, CancellationToken token = default);
    public abstract Task Hydrate(IList<TEntity> entities, CancellationToken token = default);
    public abstract Task<IQueryable<TEntity>> FilterAny(IQueryable<TEntity> query, string[] tags, CancellationToken token = default);
    public abstract Task<IReadOnlyList<string>> Distinct(CancellationToken token = default);

    /// <summary>
    /// Remove is set-minus over the current tags — one code path for all three repositories, and it keeps
    /// <see cref="Set"/>'s single write semantic (and therefore its atomicity and parent touch).
    /// </summary>
    public virtual async Task Remove(TKey id, IEnumerable<string> tags, CancellationToken token = default)
    {
        var removing = TagName.Set(tags);
        if (removing.Count == 0)
            return;

        var current = await Get(id, token);
        var remaining = current.Where(t => !removing.Contains(t)).ToList();

        if (remaining.Count != current.Count)
            await Set(id, remaining, token);
    }

    /// <summary>
    /// Re-saves the tagged row so a tag change is visible as a change OF THAT ROW. Without it a side-table tag
    /// edit is invisible to the audit log and to every client caching the row (tags grill, decision 17).
    /// No-op for entities with no update repository registered.
    /// </summary>
    protected async Task Touch(TKey id, CancellationToken token)
    {
        var update = R<IUpdateRepository<TEntity, TKey>>();
        if (update == null)
            return;

        var entity = await GetById(id, token);
        if (entity != null)
            await update.Update(entity, token);
    }

    /// <summary>
    /// Groups hydrated (id, tag) pairs onto the entities — deduped and ordinally sorted, so a side-table
    /// repository hands consumers exactly the order the inline column holds.
    /// </summary>
    protected static void Assign(IList<TEntity> entities, ILookup<TKey, string> byId)
    {
        foreach (var entity in entities)
        {
            var tags = byId[entity.Id].Distinct(StringComparer.Ordinal).Order(StringComparer.Ordinal).ToList();
            entity.Tags = tags.Count > 0 ? tags : null;
        }
    }
}
