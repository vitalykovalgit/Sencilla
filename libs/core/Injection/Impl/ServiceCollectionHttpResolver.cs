namespace Microsoft.Extensions.DependencyInjection;

[DisableInjection]
public class ServiceCollectionHttpResolver : IResolver
{
    protected readonly IHttpContextAccessor ContextAccessor;

    public ServiceCollectionHttpResolver(IHttpContextAccessor? contextAccessor)
    {
        ContextAccessor = contextAccessor ?? throw new NullReferenceException($"{nameof(contextAccessor)} could not be null");
    }

    public TServcie? Resolve<TServcie>(string name) => Resolve<TServcie>();
    public TServcie? Resolve<TServcie>() => ContextAccessor.HttpContext!.RequestServices.GetService<TServcie>();
    public object? Resolve(Type type) => ContextAccessor.HttpContext!.RequestServices.GetService(type);
    public IEnumerable<TType> ResolveAll<TType>() => ContextAccessor.HttpContext!.RequestServices.GetServices<TType>();
}