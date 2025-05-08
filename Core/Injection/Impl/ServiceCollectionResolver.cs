
namespace Microsoft.Extensions.DependencyInjection;

public class ServiceCollectionResolver : IResolver
{
    IServiceProvider Provider;

    public ServiceCollectionResolver(IServiceProvider provider)
    {
        Provider = provider;
    }

    public object? Resolve(Type type) => Provider.GetService(type);
    public TServcie? Resolve<TServcie>() => Provider.GetService<TServcie>();
    public TServcie? Resolve<TServcie>(string name) => Provider.GetKeyedService<TServcie>(name);
    public IEnumerable<TType> ResolveAll<TType>() => Provider.GetServices<TType>();
}
