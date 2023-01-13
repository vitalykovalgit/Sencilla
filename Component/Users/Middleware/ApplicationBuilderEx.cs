using Microsoft.AspNetCore.Builder;
using Sencilla.Component.Users;

namespace Microsoft.Extensions.DependencyInjection
{
    /// <summary>
    /// 
    /// </summary>
    public static class ApplicationBuilderEx
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="builder"></param>
        /// <returns></returns>
        public static IApplicationBuilder UseSencillaUser(this IApplicationBuilder builder)
        {
            return builder.UseUserRegistration();
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="builder"></param>
        /// <returns></returns>
        public static IApplicationBuilder UseUserRegistration(this IApplicationBuilder builder)
        {
            return builder.UseMiddleware<UserRegistrationMiddleware>();                          
        }
    }
}
