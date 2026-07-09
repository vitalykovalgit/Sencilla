namespace Sencilla.Component.Audit;

/// <summary>
/// An entity that records the actor (type + id) behind it. The reusable actor primitive the audit component
/// stamps; implemented by the audit-log row today, available to any entity that wants inline actor attribution.
/// </summary>
public interface IEntityAuditableTrack : IBaseEntity
{
    ActorType ActorType { get; set; }

    /// <summary>The acting user's id, or null for a System actor.</summary>
    Guid? ActorId { get; set; }
}
