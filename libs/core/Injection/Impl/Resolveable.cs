
namespace Sencilla.Core;

/// <summary>
/// TODO: Think about this class if it is needed 
/// </summary>
// ReSharper disable once InconsistentNaming
public class Resolveable(IServiceProvider resolver)
{
    protected T?      R<T>() => resolver.GetService<T>();
    protected object? R(Type type) => resolver.GetService(type);
    protected IEnumerable<T> All<T>() => resolver.GetServices<T>();
}
