
namespace Sencilla.Core;

/// <summary>
/// TODO: Think about this class if it is needed 
/// </summary>
// ReSharper disable once InconsistentNaming
public class Resolveable(IResolver Resolver)
{
    protected T?      R<T>() => Resolver.Resolve<T>();
    protected object? R(Type type) => Resolver.Resolve(type);
    protected IEnumerable<T> All<T>() => Resolver.ResolveAll<T>();
}
