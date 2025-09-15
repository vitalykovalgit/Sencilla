namespace Sencilla.Core;

/// <summary>
/// 
/// </summary>
/// <param name="provider"></param>
[DisableInjection]
public class SencillaApp(IServiceProvider provider): ISencillaApp
{
    /// <summary>
    /// 
    /// </summary>
    public IServiceProvider Provider => provider;

    /// <summary>
    /// 
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <returns></returns>
    public T Provide<T>() => Provider.GetService<T>() ?? throw new Exception($"The {typeof(T).FullName} not registered in container");
}
