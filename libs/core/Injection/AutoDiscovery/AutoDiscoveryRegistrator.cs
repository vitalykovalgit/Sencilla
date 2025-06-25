
namespace Sencilla.Core;

public class AutoDiscoveryRegistrator : ITypeRegistrator
{
    public void Register(IContainer container, Type type)
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

    IContainer RegisterType(IContainer c, bool isSingleton, bool isPerRequest, Type service, Type? impl = null)
    {
        if (isSingleton)
            return impl == null ? c.RegisterTypeSingleton(service) : c.RegisterTypeSingleton(service, impl);

        if (isPerRequest)
            return impl == null ? c.RegisterTypePerRequest(service) : c.RegisterTypePerRequest(service, impl);

        return impl == null ? c.RegisterType(service) : c.RegisterType(service, impl);
    }
}

