global using System.Reflection;

global using Microsoft.AspNetCore.Builder;
global using Microsoft.AspNetCore.Routing;
global using Microsoft.AspNetCore.Http;
global using Microsoft.AspNetCore.Http.Features;

global using Microsoft.Extensions.DependencyInjection;
global using Microsoft.Extensions.DependencyInjection.Extensions;

global using Sencilla.Web.MinimalApi;

namespace Sencilla.Web.MinimalApi;

public static class Bootstrap
{
    public static IServiceCollection AddSencillaEndpoints(this IServiceCollection services, Assembly assembly)
    {
        ServiceDescriptor[] serviceDescriptors = assembly
            .DefinedTypes
            .Where(type => type is { IsAbstract: false, IsInterface: false } &&
                           type.IsAssignableTo(typeof(IEndpoint)))
            .Select(type => ServiceDescriptor.Transient(typeof(IEndpoint), type))
            .ToArray();

        services.TryAddEnumerable(serviceDescriptors);

        return services;
    }

    //public static IApplicationBuilder MapSencillaEndpoints(this IApplicationBuilder app, RouteGroupBuilder? routeGroupBuilder = null)
    //{
    //    var endpoints = app.ApplicationServices.GetRequiredService<IEnumerable<IEndpoint>>();
    //    IEndpointRouteBuilder builder = routeGroupBuilder is null ? app : routeGroupBuilder;

    //    foreach (var endpoint in endpoints)
    //    {
    //        endpoint.MapEndpoint(builder);
    //    }

    //    return app;
    //}

    public static IEndpointRouteBuilder MapSencillaEndpoints(this IEndpointRouteBuilder builder, RouteGroupBuilder? routeGroupBuilder = null)
    {
        var endpoints = builder.ServiceProvider.GetRequiredService<IEnumerable<IEndpoint>>();
        foreach (var endpoint in endpoints)
        {
            endpoint.MapEndpoint(builder);
        }

        return builder;
    }

}
