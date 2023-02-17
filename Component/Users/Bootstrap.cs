global using System.Security.Principal;
global using System.Security.Claims;
global using System.ComponentModel.DataAnnotations.Schema;
global using Microsoft.AspNetCore.Http;
global using Microsoft.AspNetCore.Builder;
global using Microsoft.Extensions.DependencyInjection;

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
    public static IServiceCollection AddSencillaUsers(this IServiceCollection builder)
    {
        // Do nothing here
        return builder;
    }
}
