
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
        //if (context.User?.Identity?.IsAuthenticated ?? false)
        //{
        try
        {
            // get current user and check if it is not anonymous
            var container = context.RequestServices;
            var user = container.GetService<ICurrentUserProvider>()?.CurrentUser;
            if (!(user?.IsAnonymous() ?? true))
            {
                // check if user already exists
                var userReadRepo = container.GetService<IReadRepository<User>>();
                var dbUser = (await userReadRepo.GetAll(ByEmail(user.Email))).FirstOrDefault();
                if (dbUser == null)
                {
                    // create if not exists 
                    var userCreateRepo = container.GetService<ICreateRepository<User>>();
                    dbUser = await userCreateRepo.Upsert(user, u => u.Email);
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
