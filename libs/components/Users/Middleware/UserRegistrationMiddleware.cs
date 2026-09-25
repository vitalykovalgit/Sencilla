namespace Sencilla.Component.Users;

[DisableInjection]
public class UserRegistrationMiddleware
{
    private readonly RequestDelegate Next;

    private readonly IMemoryCache _cache;
    // TODO: Make this configurable
    private static readonly TimeSpan CacheExpiration = TimeSpan.FromMinutes(5);

    // ponytail: striped rather than one lock per email, so memory stays bounded for the node's lifetime.
    // Unrelated first logins that share a stripe wait for each other; raise the count if sign-up bursts show it.
    private static readonly SemaphoreSlim[] RegistrationGates = Enumerable.Range(0, 64).Select(_ => new SemaphoreSlim(1, 1)).ToArray();

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
                // GetOrCreateUserAsync) runs as the authenticated User role, not as Anonymous.
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
                        dbUser = await RegisterOnceAsync(container, userRepo!, user, userProvider.CurrentPrincipal?.Identity?.AuthenticationType, context.RequestAborted);
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


    /// <summary>
    /// First login. The page a new user lands on fires its API calls in parallel, and every one of them
    /// missed the cache and found no user. On this node one call per email creates the user and the rest
    /// read it back. Across nodes the unique keys on sec.User and sec.UserAuth keep the inserts correct;
    /// this only spares the database the pile-up.
    /// </summary>
    private async Task<User> RegisterOnceAsync(IServiceProvider sp, ICreateRepository<User, Guid> userRepo, User user, string? authType, CancellationToken token)
    {
        var gate = RegistrationGates[(StringComparer.OrdinalIgnoreCase.GetHashCode(user.Email ?? "") & int.MaxValue) % RegistrationGates.Length];
        await gate.WaitAsync(token);
        try
        {
            return await userRepo.FirstOrDefault(ByEmail(user.Email), token)
                ?? await GetOrCreateUserAsync(sp, userRepo, user, authType);
        }
        finally
        {
            gate.Release();
        }
    }

    private async Task<User> GetOrCreateUserAsync(IServiceProvider sp, ICreateRepository<User, Guid> userRepo, User user, string? authType)
    {
        // Insert-only: a row another request or node created a moment ago is read back, never rewritten.
        await userRepo.GetOrCreateAsync([user], u => u.Email!);
        var dbUser = await userRepo.FirstOrDefault(ByEmail(user.Email));

        var userAuth = new UserAuth()
        {
            UserId = dbUser.Id,
            Auth = authType ?? "",
            Email = dbUser.Email,
            CreatedDate = DateTime.UtcNow,
        };

        var userAuthRepo = sp.GetService<ICreateRepository<UserAuth, Guid>>();
        // Matched on exactly the columns of UX_UserAuth_EmailAuthUser (UserAuth.sql): with that unique index
        // under the MERGE's HOLDLOCK, concurrent first logins queue on one key instead of deadlocking.
        await userAuthRepo!.GetOrCreateAsync([userAuth], u => u.Email!, u => u.Auth, u => u.UserId);

        return dbUser;
    }
}
