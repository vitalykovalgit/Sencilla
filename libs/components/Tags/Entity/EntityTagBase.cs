namespace Sencilla.Component.Tags;

/// <summary>
/// One stored tag, in whichever table the entity's storage strategy puts it — the shape
/// <see cref="TagLinkedRepository{TEntity,TKey,TRow}"/> can work with generically. Subclasses add only the
/// addressing: a typed foreign key (<see cref="EntityTagLink{TEntity,TKey}"/>) or a type discriminator plus a
/// stringified id (<see cref="EntityTag"/>).
///
/// <para>Hard-deletable by design: a tag set is replaced by diffing rows, and an untagged row leaves nothing
/// behind to soft-delete.</para>
/// </summary>
public abstract class EntityTagBase : IEntity<long>, IEntityCreateableTrack, IEntityDeleteable
{
    public long Id { get; set; }

    /// <summary>The tag, already normalised by <see cref="TagName"/>.</summary>
    public string Name { get; set; } = "";

    public DateTime CreatedDate { get; set; }
}
