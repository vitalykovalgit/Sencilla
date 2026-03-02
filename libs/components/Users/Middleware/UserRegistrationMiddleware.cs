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
            if (!user.IsAnonymous())
            {
                var cacheKey = $"user_by_email_{user.Email}";

                // Fast path: if user is already cached, skip all DI/repo resolution
                if (_cache.TryGetValue(cacheKey, out User? dbUser) && dbUser != null)
                {
                    user = dbUser;
                }
                else
                {
                    // Cache miss — resolve repo and look up user in DB
                    var userRepo = container.GetService<ICreateRepository<User>>();
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

            // Set current user to system variable
            var sysVars = container.GetService<ISystemVariable>();
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


    private async Task<User> UpsertUserAsync(IServiceProvider sp, ICreateRepository<User> userRepo, User user, string? authType)
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

        var userAuthRepo = sp.GetService<ICreateRepository<UserAuth, byte>>();
        await userAuthRepo.UpsertAsync(userAuth, u => new { u.Email, u.Auth, u.UserId, });

        return dbUser;
    }
}
