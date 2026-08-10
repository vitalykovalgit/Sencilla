namespace Sencilla.Component.Users;

public class CurrentUserProvider: ICurrentUserProvider
{
    IHttpContextAccessor ContextAccessor;
    ISystemVariable SysVars;

    public CurrentUserProvider(IHttpContextAccessor accessor, ISystemVariable sysVars)
    {
        ContextAccessor = accessor;
        SysVars = sysVars;
    }

    /// <summary>
    /// The request's EFFECTIVE user — the one every other part of the framework acts as.
    ///
    /// System variables win over the principal. <see cref="UserRegistrationMiddleware"/> puts the
    /// persisted row there (claims alone carry no Id and no roles), and anything that re-points the
    /// current user afterwards — impersonation — must be visible here too, or this provider and
    /// <c>sysVars.GetCurrentUser()</c> disagree about who is calling. They did: everything
    /// security-, stamp- and audit-related reads system variables, while this property rebuilt the
    /// user from the ClaimsPrincipal on every call, so <c>GET api/v1/users/current</c> answered with
    /// the authenticated principal rather than the effective identity.
    ///
    /// The claims fallback still covers the window before the middleware has run (including the
    /// middleware's own first read, which is what seeds system variables in the first place).
    /// </summary>
    public User CurrentUser => SysVars.GetCurrentUser() ?? FromPrincipal();

    /// <summary>
    ///
    /// </summary>
    public IPrincipal? CurrentPrincipal => ContextAccessor.HttpContext?.User ?? Thread.CurrentPrincipal;

    /// <summary>
    /// Convert current principal to sencilla user
    /// </summary>
    /// <returns></returns>
    private User FromPrincipal()
    {
        var user = (CurrentPrincipal as ClaimsPrincipal)?.ToUser() ?? new User();

        // Add anonymous role be default
        user.AddRole((int)RoleType.Anonymous);

        // if user is not empty add general role
        if (!user.IsAnonymous())
            user.AddRole((int)RoleType.User);

        return user;
    }
}
