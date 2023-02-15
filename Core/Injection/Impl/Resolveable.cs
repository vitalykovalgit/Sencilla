
namespace Sencilla.Core;

/// <summary>
/// TODO: Think about this class if it is needed 
/// </summary>
public class Resolveable
{
    protected IResolver Resolver { get; }

    public Resolveable(IResolver resolver)
    {
        Resolver = resolver;
    }

    protected T?     R<T>() => Resolver.Resolve<T>();
    protected object R(Type type) => Resolver.Resolve(type);
    protected IEnumerable<T> All<T>() => Resolver.ResolveAll<T>();
}
