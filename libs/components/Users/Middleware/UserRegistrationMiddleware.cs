namespace Sencilla.Component.Users;

[DisableInjection]
public class UserRegistrationMiddleware
{
    private readonly RequestDelegate Next;

    public UserRegistrationMiddleware(RequestDelegate next)
    {
        Next = next;
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
                // check if user already exists
                var userRepo = container.GetService<ICreateRepository<User>>();
                var dbUser = await userRepo!.FirstOrDefault(ByEmail(user.Email));
                if (dbUser == null)
                {
                    // create if not exists 
                    dbUser = await UpsertUserAsync(container, userRepo, user, userProvider.CurrentPrincipal?.Identity?.AuthenticationType);
                }
                
                user = dbUser;
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
