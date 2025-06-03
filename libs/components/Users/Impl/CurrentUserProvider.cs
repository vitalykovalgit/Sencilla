namespace Sencilla.Component.Users;

public class CurrentUserProvider: ICurrentUserProvider
{
    //ILogger Logger;
    IHttpContextAccessor ContextAccessor;
    IReadRepository<User> ReadUserRepo;

    public CurrentUserProvider(IHttpContextAccessor accessor, IReadRepository<User> readUserRepo)
    {
        ContextAccessor = accessor;
        ReadUserRepo = readUserRepo;
    }

    /// <summary>
    /// 
    /// </summary>
    public User CurrentUser => GetCurrentUser();

    /// <summary>
    /// 
    /// </summary>
    public IPrincipal? CurrentPrincipal => ContextAccessor.HttpContext.User ?? Thread.CurrentPrincipal;

    /// <summary>
    /// Convert current principal to sencilla user 
    /// </summary>
    /// <returns></returns>
    private User GetCurrentUser()
    {
        var user = (CurrentPrincipal as ClaimsPrincipal)?.ToUser() ?? new User();

        // TODO: try read user from DB 

        // Add anonymous role be default 
        user.AddRole((int)RoleType.Anonymous);

        // if user is not empty add general role 
        if (!user.IsAnonymous())
            user.AddRole((int)RoleType.User);

        return user;
    }
}
