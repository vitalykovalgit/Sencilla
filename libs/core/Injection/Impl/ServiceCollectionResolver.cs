
namespace Microsoft.Extensions.DependencyInjection;

public class ServiceCollectionResolver(IServiceProvider provider) : IResolver
{
    public object? Resolve(Type type) => provider.GetService(type);
    public TServcie? Resolve<TServcie>() => provider.GetService<TServcie>();
    public TServcie? Resolve<TServcie>(string name) => provider.GetKeyedService<TServcie>(name);
    public IEnumerable<TType> ResolveAll<TType>() => provider.GetServices<TType>();
}
