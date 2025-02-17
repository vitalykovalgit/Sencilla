
namespace Microsoft.Extensions.DependencyInjection;

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
        return Provider.GetKeyedService<TServcie>(name);
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
