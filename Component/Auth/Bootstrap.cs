global using System.ComponentModel.DataAnnotations.Schema;
global using System.IdentityModel.Tokens.Jwt;
global using System.Security.Claims;
global using System.Text;

global using Microsoft.Extensions.Options;
global using Microsoft.AspNetCore.Identity;
global using Microsoft.IdentityModel.Tokens;

global using Sencilla.Core;
global using Sencilla.Component.Users;


[assembly: AutoDiscovery]

namespace Microsoft.Extensions.DependencyInjection;

public static class Bootstrap
{
    /// <summary>
    /// This method is used in startup class just to reference 
    /// this assambly with all it's component 
    /// Sencilla will register everything automatically
    /// </summary>
    public static IServiceCollection AddSencillaUsersRegistration(this IServiceCollection builder)
    {
        // Do nothing here
        return builder;
    }
}
