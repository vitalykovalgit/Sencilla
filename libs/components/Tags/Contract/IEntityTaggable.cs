
namespace Sencilla.Component.Tags;

/// <summary>
/// Opt-in marker: this entity carries free-text tags. The ONLY abstraction the filter pipeline
/// (<c>IFilter.Tag</c>), the generic tag endpoints and any consuming DSL program against — where the tag rows
/// physically live is chosen by which of the three storage sub-interfaces the entity also implements, and is
/// never visible on the wire.
///
/// <para><see cref="Tags"/> IS the wire contract, identical for all three repositories: <c>tags: string[]</c> on the
/// entity JSON. For <see cref="IEntityTaggableInline"/> it is a mapped column; for the other two it is ignored
/// by the model and filled after read by the tag hydration handler. That uniformity is what lets an entity
/// migrate between repositories without touching a single consumer.</para>
///
/// <para>Tags are normalised by <see cref="TagName"/> — lowercased, trimmed, deduped, ordinally sorted — so
/// ordinal (case-sensitive) comparison in a consumer and case-insensitive comparison in SQL Server can never
/// disagree. NULL means "no tags"; never an empty array. Consumers treat null, absent and non-array alike.</para>
/// </summary>
public interface IEntityTaggable : IBaseEntity
{
    /// <summary>Normalised tag set, or null when the entity has no tags.</summary>
    List<string>? Tags { get; set; }
}

/// <summary>
/// Tags live in a <c>Tags</c> column on the entity's own row, as a JSON array (EF primitive collection).
///
/// <para>Pick this when the table is small or is loaded wholesale by a consumer: the tags arrive with the row —
/// no join, no second query — and a tag edit moves the row's own <c>UpdatedDate</c> and audit trail, so cached
/// copies invalidate exactly as they do for any other field. The column is <c>NVARCHAR(4000)</c> (~62 tags);
/// <c>DynamicDbContext</c> applies the matching <c>HasMaxLength</c> so EF stops sending <c>nvarchar(-1)</c>
/// parameters at it.</para>
/// </summary>
public interface IEntityTaggableInline : IEntityTaggable
{
}

/// <summary>
/// Tags live in the entity's own link table, <c>{Entity}Tag</c> — a real typed foreign key with
/// <c>ON DELETE CASCADE</c> and a narrow index.
///
/// <para>Pick this for large or hot tables that must filter by tag in SQL rather than in memory. Costs one link
/// entity per adopter — <c>class PriceRuleTag : EntityTagLink&lt;PriceRule, Guid&gt; {}</c>, which MUST be
/// non-generic or Sencilla's auto-discovery skips it (no repository, no EF mapping) — plus its DDL.</para>
/// </summary>
public interface IEntityTaggableLinked : IEntityTaggable
{
}

/// <summary>
/// Tags live in the one shared table, <c>tag.EntityTag(Entity, EntityId, Name)</c>, discriminated by
/// <c>typeof(TEntity).Name</c>.
///
/// <para>Pick this to tag an entity with no per-entity DDL at all, or to answer cross-entity questions
/// ("everything tagged summer"). Costs referential integrity: <c>EntityId</c> is an FK-less
/// <c>NVARCHAR(64)</c>, so the ids round-trip through strings — fine for modest row counts, and
/// <see cref="IEntityTaggableLinked"/> exists for when it is not. Orphans are swept on hard delete;
/// soft-deleted rows keep their tags.</para>
/// </summary>
public interface IEntityTaggableShared : IEntityTaggable
{
}
