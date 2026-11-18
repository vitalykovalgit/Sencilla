namespace Sencilla.Component.Users;

/// <summary>
/// Makes the request's current user (<see cref="SystemVariableEx.SetCurrentUser"/>) the account its session
/// carries: the user whose id <see cref="ClaimsPrincipalEx.GetUserId"/> reads, loaded by id and cached for
/// five minutes — or <see cref="User.CreateAnonymous"/> when the session carries no id or no such user exists.
/// It creates nobody: accounts are created at sign-in.
/// </summary>
[DisableInjection]
public class CurrentUserMiddleware(RequestDelegate next, IMemoryCache cache)
{
    private static readonly TimeSpan CacheExpiration = TimeSpan.FromMinutes(5);

    public async Task Invoke(HttpContext context)
    {
        var user = context.User.GetUserId() is { } id
            ? await FindUserById(context.RequestServices, id, context.RequestAborted)
            : null;

        context.RequestServices.GetService<ISystemVariable>()?.SetCurrentUser(user ?? User.CreateAnonymous());

        await next(context);
    }

    private async Task<User?> FindUserById(IServiceProvider services, Guid id, CancellationToken token)
    {
        var cacheKey = $"user_by_id_{id}";
        if (cache.TryGetValue(cacheKey, out User? user) && user is not null)
            return user;

        // Root: nobody is the current user yet, so the caller's matrix grants cannot scope this read — the
        // authenticated session is what proves the id.
        using (Access.Root())
            user = await services.GetRequiredService<IReadRepository<User, Guid>>().FirstOrDefault(ById(id), token);

        if (user is not null)
            cache.Set(cacheKey, user, CacheExpiration);

        return user;
    }
}
