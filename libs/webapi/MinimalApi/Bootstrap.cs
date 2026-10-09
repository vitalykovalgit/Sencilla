global using System.Reflection;
global using System.Text.Json;
global using System.Text.Json.Serialization;
global using System.Threading.RateLimiting;

global using Microsoft.AspNetCore.Builder;
global using Microsoft.AspNetCore.Routing;
global using Microsoft.AspNetCore.Http;
global using Microsoft.AspNetCore.Http.Features;

global using Microsoft.Extensions.DependencyInjection;
global using Microsoft.Extensions.DependencyInjection.Extensions;
global using Microsoft.Extensions.Logging;

global using Sencilla.Core;
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

        // Minimal-API endpoints bind/serialize through Http.Json options (separate from MVC),
        // so apply the same Sencilla converter set here for parity. Idempotent.
        services.ConfigureHttpJsonOptions(o => o.SerializerOptions.AddSencillaJsonConverters());

        return services;
    }

    /// <summary>
    /// Opts the host into <see cref="ClientLogEndpoint"/> (<c>POST /api/v1/client-log</c>): browser error
    /// reports become structured log entries. Mapped by <see cref="MapSencillaEndpoints"/> like any endpoint.
    /// </summary>
    public static IServiceCollection AddClientLogging(this IServiceCollection services)
    {
        services.TryAddEnumerable(ServiceDescriptor.Transient<IEndpoint, ClientLogEndpoint>());
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
