namespace Sencilla.Component.Audit;

/// <summary>
/// Ambient per-request audit attribution: who (<see cref="ActorType"/>/<see cref="ActorId"/>), why
/// (<see cref="Reason"/>), and a <see cref="CorrelationId"/> that ties one logical operation's rows together.
/// </summary>
public interface IAuditContext
{
    ActorType ActorType { get; }
    Guid? ActorId { get; }

    /// <summary>Set by the X-Audit-Reason middleware; null when the caller supplied no reason.</summary>
    string? Reason { get; set; }

    Guid CorrelationId { get; }
}
