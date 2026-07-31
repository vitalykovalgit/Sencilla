namespace Sencilla.Component.Tags;

/// <summary>
/// Tags in the one shared table (<see cref="IEntityTaggableShared"/>), discriminated by entity type name — no
/// per-entity DDL at all.
///
/// <para>Storage-wise it is a link table whose foreign key was traded away: the rows carry
/// <c>(Entity, EntityId as text)</c> instead of a typed FK. Everything else — diffed writes, batched hydration,
/// tag filtering, autocomplete — is <see cref="TagLinkedRepository{TEntity,TKey,TRow}"/>'s, reused as-is; only
/// the four addressing seams are overridden, plus <see cref="Rows"/>, which is what scopes every one of them to
/// this entity type.</para>
///
/// <para>A row whose id no longer parses (a key type changed, a row was hand-edited) is skipped rather than
/// allowed to break a read.</para>
/// </summary>
public class TagSharedRepository<TEntity, TKey>(RepositoryDependency dependency, DynamicDbContext context)
    : TagLinkedRepository<TEntity, TKey, EntityTag>(dependency, context)
    where TEntity : class, IEntity<TKey>, IEntityTaggableShared, new()
{
    /// <summary>The shared table holds every entity's tags, so everything starts from this entity's slice.</summary>
    protected override IQueryable<EntityTag> Rows => DbContext.Set<EntityTag>().Where(r => r.Entity == EntityName);

    protected override IQueryable<EntityTag> RowsOf(IReadOnlyCollection<TKey> ids)
    {
        var keys = ids.Select(TagKey.Text).ToList();

        return Rows.Where(r => keys.Contains(r.EntityId));
    }

    protected override async Task<IReadOnlyList<OwnedTag>> Owned(IReadOnlyCollection<TKey> ids, CancellationToken token)
    {
        var rows = await RowsOf(ids).Select(r => new { r.EntityId, r.Name }).ToListAsync(token);

        var owned = new List<OwnedTag>(rows.Count);
        foreach (var row in rows)
            if (TagKey.TryParse<TKey>(row.EntityId, out var key))
                owned.Add(new OwnedTag(key, row.Name));

        return owned;
    }

    protected override async Task<List<TKey>> KeysOf(string[] tags, CancellationToken token)
    {
        var texts = await Rows.Where(r => tags.Contains(r.Name)).Select(r => r.EntityId).Distinct().ToListAsync(token);

        var keys = new List<TKey>(texts.Count);
        foreach (var text in texts)
            if (TagKey.TryParse<TKey>(text, out var key))
                keys.Add(key);

        return keys;
    }

    protected override void Add(TKey id, string name)
        => DbContext.Add(new EntityTag
        {
            Entity = EntityName,
            EntityId = TagKey.Text(id),
            Name = name,
            CreatedDate = DateTime.UtcNow
        });
}
