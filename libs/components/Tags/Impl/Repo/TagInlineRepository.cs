namespace Sencilla.Component.Tags;

/// <summary>
/// Tags in a JSON-array column on the entity's own row (<see cref="IEntityTaggableInline"/>). The cheapest of
/// the three: no join, no second query, no hydration, and the write IS an update of the tagged row — so
/// <c>UpdatedDate</c>, the audit trail and any client cache keyed on the row all move for free.
/// </summary>
public class TagInlineRepository<TEntity, TKey>(RepositoryDependency dependency, DynamicDbContext context)
    : TagReadRepository<TEntity, TKey>(dependency, context)
    where TEntity : class, IEntity<TKey>, IEntityTaggableInline, new()
{
    public override async Task<IReadOnlyList<string>> Get(TKey id, CancellationToken token = default)
        => (await GetById(id, token))?.Tags ?? [];

    public override async Task Set(TKey id, IEnumerable<string> tags, CancellationToken token = default)
    {
        var normalized = TagName.Set(tags);

        var entity = await GetById(id, token)
            ?? throw new BadRequestException("tag-entity-not-found");

        // NULL, not [], for the empty set — the column is nullable and consumers treat null as empty.
        entity.Tags = normalized.Count > 0 ? normalized : null;

        var update = R<IUpdateRepository<TEntity, TKey>>()
            ?? throw new BadRequestException("tag-entity-not-updateable");

        await update.Update(entity, token);
    }

    /// <summary>Nothing to do — the column came back with the row.</summary>
    public override Task Hydrate(IList<TEntity> entities, CancellationToken token = default) => Task.CompletedTask;

    public override Task<IQueryable<TEntity>> FilterAny(IQueryable<TEntity> query, string[] tags, CancellationToken token = default)
    {
        if (tags.Length == 0)
            return Task.FromResult(query);

        // An OR chain of Contains rather than Tags.Any(t => wanted.Contains(t)): Contains on a primitive
        // collection is the shape verified to translate (EntityModelConfiguratorTests), and it parameterises
        // each tag.
        Expression<Func<TEntity, bool>>? predicate = null;
        foreach (var tag in tags)
        {
            var wanted = tag;
            Expression<Func<TEntity, bool>> one = e => e.Tags != null && e.Tags.Contains(wanted);
            predicate = predicate == null ? one : predicate.OrElse(one);
        }

        return Task.FromResult(query.Where(predicate!));
    }

    /// <summary>
    /// Reads through the entity pipeline (<c>QueryAsync</c>), so the autocomplete only ever offers tags from
    /// rows the caller may see. The flattening is client-side: a JSON collection column has no DISTINCT to push
    /// down, which is the inline storage's trade — move an entity that outgrows it to
    /// <see cref="TagLinkedRepository{TEntity,TKey,TRow}"/>, whose tag table distincts in SQL.
    /// </summary>
    public override async Task<IReadOnlyList<string>> Distinct(CancellationToken token = default)
    {
        var query = await QueryAsync(token);
        var sets = await query.AsNoTracking().Where(e => e.Tags != null).Select(e => e.Tags!).ToListAsync(token);

        return [.. sets.SelectMany(t => t).Distinct(StringComparer.Ordinal).Order(StringComparer.Ordinal)];
    }
}
