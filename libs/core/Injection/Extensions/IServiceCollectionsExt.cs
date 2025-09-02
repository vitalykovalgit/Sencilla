
namespace Microsoft.Extensions.DependencyInjection;

public static class SencillaAutodiscoveryServiceCollectionExtensions
{
    public static IServiceCollection AddSencillaAutoDiscoveryFor<T>(this IServiceCollection services)
    {
        return services.AddSencillaAutoDiscoveryFor(typeof(T));
    }

    public static IServiceCollection AddSencillaAutoDiscoveryFor<T>(this IServiceCollection services, Assembly assembly)
    {
        return services.AddSencillaAutoDiscoveryFor(assembly, typeof(T));
    }

    public static IServiceCollection AddSencillaAutoDiscoveryFor(this IServiceCollection services, Type type)
    {
        // Get calling assembly
        var assembly = new StackFrame(2).GetMethod()?.DeclaringType?.Assembly;
        return services.AddSencillaAutoDiscoveryFor(assembly, type);
    }

    /// <summary>
    /// Adds Sencilla auto-discovery for the specified assembly and interfaces.
    /// 
    /// </summary>
    /// <param name="services"></param>
    /// <param name="assembly"></param>
    /// <param name="interfaces"></param>
    /// <returns></returns>
    public static IServiceCollection AddSencillaAutoDiscoveryFor(this IServiceCollection services, Assembly? assembly, params Type[] interfaces)
    {
        if (assembly == null)
            return services;

        foreach (var type in assembly.GetTypes().Where(t => t.IsClass && !t.IsAbstract && !t.IsGenericType))
            foreach (var @interface in interfaces)
            {
                if (@interface.IsGenericTypeDefinition)
                {
                    var implementedInterfaces = type.GetInterfaces().Where(i => i.IsGenericType && i.GetGenericTypeDefinition() == @interface);
                    if (implementedInterfaces.Any())
                    {
                        services.TryAddTransient(type);
                        foreach (var implementedInterface in implementedInterfaces)
                            services.TryAddTransient(implementedInterface, type);
                    }
                }
                else if (@interface.IsAssignableFrom(type))
                {
                    services.TryAddTransient(type);
                    services.TryAddTransient(@interface, type);
                }
            }

        return services;
    }
}