namespace Sencilla.Component.Tags;

/// <summary>
/// Tags as rows in the entity's own link table (<see cref="IEntityTaggableLinked"/>): a typed FK, a narrow
/// index and <c>ON DELETE CASCADE</c>, so tag rows die with their parent and nothing needs sweeping.
///
/// <para>This is also the base of <see cref="TagSharedRepository{TEntity,TKey}"/> — everything row-shaped
/// (diffed writes, batched hydration, tag filtering, autocomplete) is written once here and reused, with four
/// small <c>virtual</c> seams for the parts that genuinely differ: which rows belong to this entity, how a row
/// addresses its parent, and how a new row is built. The link table's own key is read and written through EF's
/// model by NAME (<c>EF.Property</c>), which is what lets one implementation serve any adopter's link entity
/// without knowing its static shape.</para>
/// </summary>
/// <typeparam name="TRow">The adopter's concrete link entity, e.g. <c>PriceRuleTag</c>.</typeparam>
public class TagLinkedRepository<TEntity, TKey, TRow>(RepositoryDependency dependency, DynamicDbContext context)
    : TagReadRepository<TEntity, TKey>(dependency, context)
    where TEntity : class, IEntity<TKey>, IEntityTaggable, new()
    where TRow : EntityTagBase, new()
{
    /// <summary>The FK column, by name — see the class remarks.</summary>
    protected const string EntityIdProperty = "EntityId";

    /// <summary>Every tag row this repository owns. The shared table narrows it to one entity type.</summary>
    protected virtual IQueryable<TRow> Rows => DbContext.Set<TRow>();

    public override async Task<IReadOnlyList<string>> Get(TKey id, CancellationToken token = default)
        => [.. (await Owned([id], token)).Select(t => t.Name).Distinct(StringComparer.Ordinal).Order(StringComparer.Ordinal)];

    /// <summary>
    /// Replace-set, applied as a DIFF: only genuinely new tags are inserted and only dropped ones deleted, so
    /// re-saving an unchanged set writes nothing at all — no row churn and, more importantly, no audit noise on
    /// the parent. Atomic (the framework's ambient-transaction helper), because a half-applied tag set on a
    /// pricing input would silently re-price.
    /// </summary>
    public override async Task Set(TKey id, IEnumerable<string> tags, CancellationToken token = default)
    {
        var normalized = TagName.Set(tags);
        var existing = await RowsOf([id]).ToListAsync(token);

        var current = existing.Select(r => r.Name).ToHashSet(StringComparer.Ordinal);
        var removing = existing.Where(r => !normalized.Contains(r.Name)).ToList();
        var adding = normalized.Where(t => !current.Contains(t)).ToList();

        if (removing.Count == 0 && adding.Count == 0)
            return;

        await InTransaction(async () =>
        {
            if (removing.Count > 0)
                DbContext.RemoveRange(removing);

            foreach (var name in adding)
                Add(id, name);

            await Save(token);
            await Touch(id, token);
            return true;
        }, token);
    }

    /// <summary>One query for the whole page — the reason <c>EntityReadEvent</c> is collection-shaped.</summary>
    public override async Task Hydrate(IList<TEntity> entities, CancellationToken token = default)
    {
        if (entities.Count == 0)
            return;

        var owned = await Owned([.. entities.Select(e => e.Id).Distinct()], token);

        Assign(entities, owned.ToLookup(t => t.Key, t => t.Name));
    }

    public override async Task<IQueryable<TEntity>> FilterAny(IQueryable<TEntity> query, string[] tags, CancellationToken token = default)
    {
        if (tags.Length == 0)
            return query;

        var ids = await KeysOf(tags, token);

        // List<TKey>.Contains translates to IN for a generic key (ReadRepository.GetByIds relies on the same
        // shape); an empty list correctly matches nothing.
        // ponytail: an IN list is the ceiling — swap for a correlated EXISTS if a taggable entity ever matches
        // tens of thousands of rows.
        return query.Where(e => ids.Contains(e.Id));
    }

    /// <summary>Distinct in SQL; ordered in memory, because ordinal order is the contract and the database's collation is not.</summary>
    public override async Task<IReadOnlyList<string>> Distinct(CancellationToken token = default)
    {
        var names = await Rows.Select(r => r.Name).Distinct().ToListAsync(token);

        return [.. names.Order(StringComparer.Ordinal)];
    }

    /// <summary>
    /// Drops every tag row of the given parents in one statement, with no parent touch — for the case where the
    /// parents themselves are going away. Only the shared table needs it (a link table cascades, an inline
    /// column goes with its row).
    /// </summary>
    public async Task Clear(IReadOnlyCollection<TKey> ids, CancellationToken token = default)
    {
        if (ids.Count == 0)
            return;

        await RowsOf(ids).ExecuteDeleteAsync(token);
    }

    // ── The four seams the shared table overrides ────────────────────────────────────────────────────────

    /// <summary>The rows belonging to these parents.</summary>
    protected virtual IQueryable<TRow> RowsOf(IReadOnlyCollection<TKey> ids)
        => Rows.Where(r => ids.Contains(EF.Property<TKey>(r, EntityIdProperty)));

    /// <summary>(parent, tag) pairs for these parents — projected, so no row is materialised whole.</summary>
    protected virtual async Task<IReadOnlyList<OwnedTag>> Owned(IReadOnlyCollection<TKey> ids, CancellationToken token)
    {
        var rows = await RowsOf(ids)
            .Select(r => new { Key = EF.Property<TKey>(r, EntityIdProperty), r.Name })
            .ToListAsync(token);

        return [.. rows.Select(r => new OwnedTag(r.Key, r.Name))];
    }

    /// <summary>The parents carrying ANY of these tags.</summary>
    protected virtual async Task<List<TKey>> KeysOf(string[] tags, CancellationToken token)
        => await Rows.Where(r => tags.Contains(r.Name))
                     .Select(r => EF.Property<TKey>(r, EntityIdProperty))
                     .Distinct()
                     .ToListAsync(token);

    /// <summary>
    /// Tracks a new tag row. The FK is set through the change tracker rather than the CLR property, which is
    /// what keeps this class free of the link entity's static shape. <c>CreatedDate</c> is stamped here because
    /// these rows are written through the context, not through a create repository.
    /// </summary>
    protected virtual void Add(TKey id, string name)
    {
        var entry = DbContext.Add(new TRow { Name = name, CreatedDate = DateTime.UtcNow });

        entry.Property(EntityIdProperty).CurrentValue = id;
    }

    /// <summary>One tag of one parent, as read back from the store.</summary>
    protected readonly record struct OwnedTag(TKey Key, string Name);
}
