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
                var userReadRepo = container.GetService<IReadRepository<User>>();
                var dbUser = (await userReadRepo.GetAll(ByEmail(user.Email))).FirstOrDefault();
                if (dbUser == null)
                {
                    // create if not exists 
                    var userRepo = container.GetService<ICreateRepository<User>>();
                    dbUser = await userRepo.Upsert(user, u => u.Email);

                    var userAuthRepo = container.GetService<ICreateRepository<UserAuth, byte>>();
                    var userAuth = new UserAuth
                    {
                        UserId = dbUser.Id,
                        Auth = userProvider.CurrentPrincipal?.Identity?.AuthenticationType ?? "",
                        Email = dbUser.Email,
                        CreatedDate = DateTime.UtcNow,
                    };
                    await userAuthRepo.Upsert(userAuth, u => u.Email);
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
}
