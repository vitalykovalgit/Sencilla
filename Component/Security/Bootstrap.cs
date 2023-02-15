global using System.Reflection;
global using System.Linq.Dynamic.Core;

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
    public static IServiceCollection AddSecurity(this IServiceCollection builder)
    {
        // Do nothing here
        return builder;
    }
}
