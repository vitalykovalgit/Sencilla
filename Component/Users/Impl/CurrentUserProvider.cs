using System.Security.Claims;
using System.Security.Principal;
using Microsoft.AspNetCore.Http;
using Sencilla.Core;

namespace Sencilla.Component.Users.Impl
{
    [Implement(typeof(ICurrentUserProvider))]
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

        private User GetCurrentUser()
        {
            var user = (CurrentPrincipal as ClaimsPrincipal)?.ToUser() ?? new User();

            // try read user from DB 
            user.AddRole((int)RoleType.Anonymous);

            return user;
        }
    }
}
