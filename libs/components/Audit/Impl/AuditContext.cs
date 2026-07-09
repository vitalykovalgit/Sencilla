namespace Sencilla.Component.Audit;

/// <summary>
/// Per-request audit attribution. The actor is resolved lazily from the current user (set by
/// UserRegistrationMiddleware) so it reflects the user regardless of construction order; the reason is set by
/// <see cref="AuditReasonMiddleware"/>; the correlation id is fresh per request.
/// </summary>
[PerRequestLifetime]
public class AuditContext(ISystemVariable sysVars) : IAuditContext
{
    public ActorType ActorType => ResolveActorType(sysVars.GetCurrentUser());

    public Guid? ActorId => ActorIdOf(sysVars.GetCurrentUser());

    public string? Reason { get; set; }

    public Guid CorrelationId { get; } = Guid.NewGuid();

    /// <summary>System when there is no request user; Admin for staff/root roles; otherwise User.</summary>
    public static ActorType ResolveActorType(User? user)
    {
        if (user is null)
            return ActorType.System;

        var roles = user.Roles;
        if (roles != null && roles.Any(r => r.RoleId == (int)RoleType.Admin || r.RoleId == (int)RoleType.Root))
            return ActorType.Admin;

        return ActorType.User;
    }

    static Guid? ActorIdOf(User? user) => user == null || user.Id == Guid.Empty ? null : user.Id;
}
