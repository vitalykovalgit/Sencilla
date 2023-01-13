using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Sencilla.Core;

namespace Sencilla.Component.Users
{
    public class UserRegistrationMiddleware
    {
        private readonly RequestDelegate Next;

        public UserRegistrationMiddleware(RequestDelegate next)
        {
            Next = next;
        }

        public async Task Invoke(HttpContext context)
        {
            if (context.User?.Identity?.IsAuthenticated ?? false)
            {
                try
                {
                    var container = context.RequestServices;

                    // get current user and check if it is not anonymous
                    var currentUser = container.GetService<ICurrentUserProvider>()?.CurrentUser;
                    if (!(currentUser?.IsAnonymous() ?? true))
                    {
                        // check if user already exists
                        var userReadRepo = container.GetService<IReadRepository<User>>();
                        var user = (await userReadRepo.GetAll(new UserFilter().ByEmail(currentUser.Email))).FirstOrDefault();
                        if (user == null)
                        {
                            // create if not exists 
                            var userCreateRepo = container.GetService<ICreateRepository<User>>();
                            await userCreateRepo.Create(currentUser);
                        }
                    }
                }
                catch (Exception ex)
                {
                    // For debug purpose
                    throw;
                }
            }

            await Next(context);
        }
    }
}
