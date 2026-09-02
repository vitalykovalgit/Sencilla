namespace Sencilla.Messaging;

/// <summary>
/// Maps a payload type to the string a transport puts on the wire, and back.
///
/// The key is the <see cref="PayloadTypeAttribute"/> alias when the type declares one, otherwise
/// its <c>FullName</c>. Aliases exist because durable transports persist this string forever: a
/// row written today must still resolve after the C# type is renamed or moved to another assembly.
/// For the same reason the key is deliberately NOT assembly-qualified.
///
/// Resolution tries <see cref="Type.GetType(string)"/> first (fast path, and all it can ever find
/// is a type in Sencilla.Messaging or corelib), then scans loaded assemblies once per key. Misses
/// are cached too — an unresolvable key must fail fast, not re-scan every message.
/// </summary>
public static class PayloadTypeRegistry
{
    private static readonly ConcurrentDictionary<string, Type?> Cache = [];

    /// <summary>The wire key for a payload type.</summary>
    public static string KeyOf(Type type)
        => type.GetCustomAttribute<PayloadTypeAttribute>(false)?.Name ?? type.FullName ?? type.Name;

    /// <summary>The wire key for a payload type.</summary>
    public static string KeyOf<T>() => KeyOf(typeof(T));

    /// <summary>The payload type for a wire key, or null when nothing in this process declares it.</summary>
    public static Type? Resolve(string key) => Cache.GetOrAdd(key, Scan);

    private static Type? Scan(string key)
    {
        var direct = Type.GetType(key, throwOnError: false);
        if (direct != null) return direct;

        foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
        {
            foreach (var type in SafeTypes(assembly))
            {
                if (type.IsAbstract || type.IsInterface) continue;
                if (KeyOf(type) == key) return type;
            }
        }

        return null;
    }

    private static IEnumerable<Type> SafeTypes(Assembly assembly)
    {
        // A partially-loadable assembly still yields the types that did load.
        try { return assembly.GetTypes(); }
        catch (ReflectionTypeLoadException ex) { return ex.Types.Where(t => t != null)!; }
        catch { return []; }
    }
}
