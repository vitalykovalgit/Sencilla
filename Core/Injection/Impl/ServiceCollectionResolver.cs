
using Sencilla.Core;

namespace Microsoft.Extensions.DependencyInjection
{
    public class ServiceCollectionRegistrator : IContainer
    {
        IServiceCollection Container;

        public ServiceCollectionRegistrator(IServiceCollection container)
        {
            Container = container;
        }

        public void RegisterInstance(Type @interface, object instance)
        {
            Container.AddSingleton(@interface, instance);
        }

        public void RegisterInstance<TInterface>(TInterface instance) where TInterface : class
        {
            Container.AddSingleton<TInterface>(instance);
        }

        public void RegisterType<TInterface, TImplementation>(string? name = null)
            where TInterface : class
            where TImplementation : class, TInterface
        {
            Container.AddTransient<TInterface, TImplementation>();
        }

        public void RegisterType(Type @interface, Type implementation)
        {
            Container.AddTransient(@interface, implementation);
        }

        public void RegisterType(Type @interface, Type implementation, string? name)
        {
            Container.AddTransient(@interface, implementation);
        }

    }

    public class ServiceCollectionResolver : IResolver
    {
        IServiceProvider Provider;

        public ServiceCollectionResolver(IServiceProvider provider)
        {
            Provider = provider;
        }

        public TServcie? Resolve<TServcie>()
        {
            return Provider.GetService<TServcie>();
        }

        public TServcie? Resolve<TServcie>(string name)
        {
            return Resolve<TServcie>();
        }

        public object? Resolve(Type type)
        {
            return Provider.GetService(type);
        }

        public IEnumerable<TType> ResolveAll<TType>()
        {
            return Provider.GetServices<TType>();
        }
    }
}
