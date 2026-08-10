namespace Sencilla.Component.Audit;

/// <summary>
/// Per-request audit attribution. The actor is resolved lazily from the current user (set by
/// UserRegistrationMiddleware, re-pointed by ImpersonationMiddleware) so it reflects the user regardless of
/// construction order; the reason is set by <see cref="AuditReasonMiddleware"/>; the correlation id is fresh
/// per request.
/// </summary>
[PerRequestLifetime]
public class AuditContext(ISystemVariable sysVars, IUserRoleResolver roles, IImpersonationContext impersonation) : IAuditContext
{
    public ActorType ActorType => ResolveActorType();

    public Guid? ActorId => ActorIdOf(sysVars.GetCurrentUser());

    public Guid? ImpersonatedById => impersonation.ImpersonatorId;

    public string? Reason { get; set; }

    public Guid CorrelationId { get; } = Guid.NewGuid();

    /// <summary>
    /// System when there is no request user; Admin for staff/root roles or any impersonated request;
    /// otherwise User.
    ///
    /// Roles come from <see cref="IUserRoleResolver"/>, NOT from <c>user.Roles</c>: the current user is
    /// loaded without that navigation, so reading the collection classified every admin's change as an
    /// ordinary user's. Under impersonation the effective user is by definition not the operator, so the
    /// role lookup would answer about the wrong person — an impersonated request is an admin action.
    /// </summary>
    ActorType ResolveActorType()
    {
        var user = sysVars.GetCurrentUser();
        if (user is null)
            return ActorType.System;

        if (impersonation.IsImpersonating)
            return ActorType.Admin;

        var roleIds = roles.Resolve(user);

        return roleIds.Contains((int)RoleType.Admin) || roleIds.Contains((int)RoleType.Root)
            ? ActorType.Admin
            : ActorType.User;
    }

    /// <summary>Role-set classification, for callers holding a user rather than a request.</summary>
    public static ActorType ResolveActorType(User? user)
    {
        if (user is null)
            return ActorType.System;

        var userRoles = user.Roles;
        if (userRoles != null && userRoles.Any(r => r.RoleId == (int)RoleType.Admin || r.RoleId == (int)RoleType.Root))
            return ActorType.Admin;

        return ActorType.User;
    }

    static Guid? ActorIdOf(User? user) => user == null || user.Id == Guid.Empty ? null : user.Id;
}
