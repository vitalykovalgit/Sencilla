namespace Sencilla.Component.Audit;

/// <summary>
/// One app-wide change-log row (audit.Audit). Type-agnostic: <see cref="EntityType"/> + <see cref="EntityId"/>
/// identify the audited row (string id fits Guid or int keys); <see cref="Changes"/> is the field-level
/// {field:{old,new}} JSON. Actor + <see cref="Reason"/> + <see cref="CorrelationId"/> answer who/why and tie a
/// multi-row operation (e.g. void-and-replace) together. Append-only by convention; never itself audited.
/// </summary>
[Table("Audit", Schema = "audit")]
public class Audit : IEntity<long>, IEntityCreateableTrack, IEntityAuditableTrack
{
    public long Id { get; set; }

    /// <summary>The audited entity's type name (e.g. "Order").</summary>
    public string EntityType { get; set; } = "";

    /// <summary>The audited row's Id, stringified (Guid or int).</summary>
    public string EntityId { get; set; } = "";

    public AuditAction Action { get; set; }

    public ActorType ActorType { get; set; }
    public Guid? ActorId { get; set; }

    /// <summary>Field-level change JSON: {field:{old,new}} (insert: old null; delete: new null).</summary>
    public string? Changes { get; set; }

    /// <summary>Optional operator-supplied reason (X-Audit-Reason header).</summary>
    public string? Reason { get; set; }

    /// <summary>Groups the rows of one logical operation.</summary>
    public Guid CorrelationId { get; set; }

    public DateTime CreatedDate { get; set; }
}
