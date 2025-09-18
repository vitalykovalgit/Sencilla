global using System.ComponentModel.DataAnnotations.Schema;

global using Sencilla.Core;
global using Sencilla.Web;

[assembly: AutoDiscovery]

namespace Microsoft.Extensions.DependencyInjection;

public static class Bootstrap 
{
    /// <summary>
    /// 
    /// </summary>
    /// <param name="services"></param>
    /// <returns></returns>
    public static IServiceCollection AddGeography(this IServiceCollection services)
    {
        return services;
    }
}
