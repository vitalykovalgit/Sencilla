namespace Sencilla.Component.Tags;

/// <summary>
/// A tag row in the shared table (<c>tag.EntityTag</c>) — the storage behind
/// <see cref="IEntityTaggableShared"/>. Type-agnostic in the same way <c>audit.Audit</c> is:
/// <see cref="Entity"/> + <see cref="EntityId"/> identify the tagged row, with no foreign key, so an entity can
/// be tagged without shipping a line of DDL.
///
/// <para><see cref="Entity"/> is <c>typeof(TEntity).Name</c> (PascalCase, as in the audit log) — deliberately
/// unlike <c>PriceRule.Entity</c>'s camelCase subject names, which is a different column in a different table.</para>
/// </summary>
[Table("EntityTag", Schema = "tag")]
public class EntityTag : EntityTagBase
{
    /// <summary>The tagged entity's type name (e.g. "PriceRule").</summary>
    public string Entity { get; set; } = "";

    /// <summary>The tagged row's id, stringified (Guid or int) — the price of having no foreign key.</summary>
    public string EntityId { get; set; } = "";
}
