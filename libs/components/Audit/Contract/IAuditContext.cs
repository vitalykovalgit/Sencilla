namespace Sencilla.Component.Audit;

/// <summary>
/// Ambient per-request audit attribution: who (<see cref="ActorType"/>/<see cref="ActorId"/>), why
/// (<see cref="Reason"/>), and a <see cref="CorrelationId"/> that ties one logical operation's rows together.
/// </summary>
public interface IAuditContext
{
    ActorType ActorType { get; }
    Guid? ActorId { get; }

    /// <summary>
    /// The REAL operator when <see cref="ActorId"/> is an impersonated account; null on an ordinary request.
    ///
    /// Under impersonation the stamps and <see cref="ActorId"/> deliberately record the impersonated user —
    /// the data must read consistently to that customer — which leaves this column as the only record that
    /// someone else performed the change. It is a column rather than a note in <see cref="Reason"/> because
    /// "what did operator X do while impersonating?" has to be answerable by a query.
    /// </summary>
    Guid? ImpersonatedById { get; }

    /// <summary>Set by the X-Audit-Reason middleware; null when the caller supplied no reason.</summary>
    string? Reason { get; set; }

    Guid CorrelationId { get; }
}
