
namespace Sencilla.Core;

public class AutoDiscoveryRegistrator : ITypeRegistrator
{
    public void Register(IServiceCollection container, Type type)
    {
        if (type.IsClass && !type.IsAbstract && !type.IsGenericType && !type.IsAssignableTo(typeof(Attribute)))
        {
            // ignore type registrators 
            if (type.IsAssignableTo(typeof(ITypeRegistrator)))
                return;

            // if injection disabled ignore this type
            var disableInjection = type.GetCustomAttributes(typeof(DisableInjectionAttribute), false).Any();
            if (disableInjection)
                return;

            // validate constructor
            var ps = type.GetConstructors().FirstOrDefault()?.GetParameters();
            var isConstructorOk = ps?.All(p => !p.ParameterType.IsPrimitive && p.ParameterType != typeof(string) && (p.ParameterType.IsClass || p.ParameterType.IsInterface)) ?? false;
            if (!isConstructorOk)
                return;

            // 
            var isSingleton = type.GetCustomAttributes(typeof(SingletonLifetimeAttribute), false).Any();
            var isPerRequest = type.GetCustomAttributes(typeof(PerRequestLifetimeAttribute), false).Any();

            // get direct interfaces 
            var interfaces = type.BaseType == null 
                            ? type.GetInterfaces()
                            : type.GetInterfaces().Except(type.BaseType.GetInterfaces());

            // register direct interfaces 
            foreach (var i in interfaces)
                RegisterType(container, isSingleton, isPerRequest, i, type);

            // register type itself
            RegisterType(container, isSingleton, isPerRequest, type);
        }
    }

    IServiceCollection RegisterType(IServiceCollection c, bool isSingleton, bool isPerRequest, Type service, Type? impl = null)
    {
        if (isSingleton)
        {
            if (impl is null) c.AddSingleton(service); else c.AddSingleton(service, impl);
            return c;
        }

        if (isPerRequest)
        {
            if (impl is null) c.AddScoped(service); else c.AddScoped(service, impl);
            return c;
        }

        if (impl is null)
            c.AddTransient(service);
        else
            c.AddTransient(service, impl);

        return c;
    }
}

