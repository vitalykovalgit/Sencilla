using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;

namespace Sencilla.Core;

/// <summary>
/// This is considered as antipatter!!
/// Use it with causion only in exceptional situations 
/// </summary>
public static class ServiceLocator
{
    private static IResolver? mResolver;
    private static IResolver Resolver => mResolver ?? throw new Exception("You should Initialize the ServiceProvider before using it.");

    public static T? R<T>() => Resolver.Resolve<T>();
    public static object R(Type type) => Resolver.Resolve(type);
    public static IEnumerable<T> RAll<T>() => Resolver.ResolveAll<T>();

    public static void UseSencillaServiceLocator(this IApplicationBuilder app)
    {
        var httpAccessor = app.ApplicationServices.GetService<IHttpContextAccessor>();
        mResolver = new ServiceCollectionHttpResolver(httpAccessor);
    }
}
