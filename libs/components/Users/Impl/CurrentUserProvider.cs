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
    /// The request's EFFECTIVE user — the one every other part of the framework acts as: the user system
    /// variables hold (<see cref="CurrentUserMiddleware"/> puts the session's account there, impersonation
    /// re-points it), else the anonymous user. It is never rebuilt from the principal's claims, so this
    /// provider and <c>sysVars.GetCurrentUser()</c> cannot disagree about who is calling.
    /// </summary>
    public User CurrentUser => SysVars.GetCurrentUser() ?? User.CreateAnonymous();

    /// <summary>
    ///
    /// </summary>
    public IPrincipal? CurrentPrincipal => ContextAccessor.HttpContext?.User ?? Thread.CurrentPrincipal;
}
