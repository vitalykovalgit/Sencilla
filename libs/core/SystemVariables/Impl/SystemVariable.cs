
namespace Sencilla.Core;

/// <summary>
/// 
/// </summary>
[PerRequestLifetime]
public class SystemVariable : ISystemVariable
{
    // TODO: think how to remove convertion to object!
    readonly Dictionary<string, object?> Variables = new();

    /// <summary>
    /// Add/rewrite variable in system variables
    /// </summary>
    /// <param name="name"></param>
    /// <param name="data"></param>
    public void Set(string name, object? data)
    {
        if (string.IsNullOrWhiteSpace(name))
            return;

        Variables[name.ToLower()] = data;
    }

    /// <summary>
    /// The same as Get<T> where T is object?
    /// </summary>
    public object? Get(string? name) => Get<object?>(name);

    /// <summary>
    /// 
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="name">name of variable, case insencitive</param>
    /// <returns></returns>
    public T? Get<T>(string? name)
    {
        if (string.IsNullOrWhiteSpace(name))
            return default;

        Variables.TryGetValue(name.ToLower(), out var obj);
        return (T?)obj;
    }
}
