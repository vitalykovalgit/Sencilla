
namespace Microsoft.Extensions.DependencyInjection;

public static class SencillaAutodiscoveryServiceCollectionExtensions
{
    public static IServiceCollection AddSencillaAutoDiscovery(this IServiceCollection services, Action<AutoDiscoveryOptions> configure)
    {
        // calling asseambly
        var assembly = new StackFrame(2).GetMethod()?.DeclaringType?.Assembly;

        var options = new AutoDiscoveryOptions { CallingAssembly = assembly! };
        configure(options);

        foreach (var kvp in options.Types)
        foreach (var type in kvp.Key.GetTypes().Where(t => t.IsClass && !t.IsAbstract && !t.IsGenericType))
        foreach (var @inteface in kvp.Value)
        {
            RegisterType(services, type, inteface);
        }

        return services;
    }

    private static void RegisterType(IServiceCollection services, Type type, Type @interface)
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
}