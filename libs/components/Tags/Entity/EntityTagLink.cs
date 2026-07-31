namespace Sencilla.Component.Tags;

/// <summary>
/// A tag row in the entity's own link table — the storage behind <see cref="IEntityTaggableLinked"/>. One
/// concrete subclass per adopter, which is the whole adoption cost:
///
/// <code>
/// [Table("PriceRuleTag")]
/// public class PriceRuleTag : EntityTagLink&lt;PriceRule, Guid&gt; { }
/// </code>
///
/// <para>The subclass MUST be non-generic: Sencilla's auto-discovery skips generic types
/// (<c>RegisterEFRepositoriesForType</c> and friends guard on <c>!type.IsGenericType</c>), so a closed generic
/// would silently get no repository and no EF mapping.</para>
///
/// <para><see cref="EntityId"/> is a real typed foreign key to the parent, declared with
/// <c>ON DELETE CASCADE</c> in the adopter's DDL — which is why this repository needs no orphan sweeping.</para>
/// </summary>
/// <typeparam name="TEntity">The tagged entity.</typeparam>
/// <typeparam name="TKey">Its primary key type.</typeparam>
public abstract class EntityTagLink<TEntity, TKey> : EntityTagBase
    where TEntity : class, IEntity<TKey>
{
    /// <summary>
    /// FK to the tagged row. Read and written generically (by property NAME, through EF's model) so one
    /// repository can serve every link table, which makes the name load-bearing.
    /// </summary>
    public TKey EntityId { get; set; } = default!;

    /// <summary>Navigation to the tagged row. Optional — nothing in the tag pipeline needs it.</summary>
    [ForeignKey(nameof(EntityId))]
    public TEntity? Entity { get; set; }
}
