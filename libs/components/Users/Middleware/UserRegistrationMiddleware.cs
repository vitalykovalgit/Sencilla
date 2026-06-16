namespace Sencilla.Component.Users;

[DisableInjection]
public class UserRegistrationMiddleware
{
    private readonly RequestDelegate Next;

    private readonly IMemoryCache _cache;
    // TODO: Make this configurable
    private static readonly TimeSpan CacheExpiration = TimeSpan.FromMinutes(5);

    public UserRegistrationMiddleware(RequestDelegate next, IMemoryCache cache)
    {
        Next = next;
        _cache = cache;
    }

    public async Task Invoke(HttpContext context)
    {
        try
        {
            // get current user and check if it is not anonymous
            var container = context.RequestServices;
            var userProvider = container.GetService<ICurrentUserProvider>();
            if (userProvider == null)
                return;

            var user = userProvider.CurrentUser;
            var sysVars = container.GetService<ISystemVariable>();
            if (!user.IsAnonymous())
            {
                // Establish the security context from the verified principal BEFORE any
                // repo work, so first-login self-registration (the User/UserAuth insert in
                // UpsertUserAsync) runs as the authenticated User role, not as Anonymous.
                // Without this the entity-constraint handler sees no current user and the
                // insert is forbidden (403). Overwritten with the persisted user below.
                sysVars?.SetCurrentUser(user);

                var cacheKey = $"user_by_email_{user.Email}";

                // Fast path: if user is already cached, skip all DI/repo resolution
                if (_cache.TryGetValue(cacheKey, out User? dbUser) && dbUser != null)
                {
                    user = dbUser;
                }
                else
                {
                    // Cache miss — resolve repo and look up user in DB
                    var userRepo = container.GetService<ICreateRepository<User, Guid>>();
                    dbUser = await userRepo!.FirstOrDefault(ByEmail(user.Email), context.RequestAborted);

                    if (dbUser == null)
                    {
                        // create if not exists
                        dbUser = await UpsertUserAsync(container, userRepo!, user, userProvider.CurrentPrincipal?.Identity?.AuthenticationType);
                    }

                    _cache.Set(cacheKey, dbUser, CacheExpiration);
                    user = dbUser;
                }
            }

            // Set current user (now the persisted record) to system variable
            sysVars?.SetCurrentUser(user);

        }
        catch //(Exception ex)
        {
            // For debug purpose
            throw;
        }
        //}

        await Next(context);
    }


    private async Task<User> UpsertUserAsync(IServiceProvider sp, ICreateRepository<User, Guid> userRepo, User user, string? authType)
    {
        // TODO: Think about saving this in transaction
        await userRepo.UpsertAsync(user, u => u.Email!);
        var dbUser = await userRepo.FirstOrDefault(ByEmail(user.Email));

        var userAuth = new UserAuth()
        {
            UserId = dbUser.Id,
            Auth = authType ?? "",
            Email = dbUser.Email,
            CreatedDate = DateTime.UtcNow,
        };

        var userAuthRepo = sp.GetService<ICreateRepository<UserAuth, Guid>>();
        await userAuthRepo.UpsertAsync(userAuth, u => new { u.Email, u.Auth, u.UserId, });

        return dbUser;
    }
}
