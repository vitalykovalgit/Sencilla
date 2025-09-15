
public class AutoDiscoveryOptions
{
    public required Assembly CallingAssembly { get; set; }
    public Dictionary<Assembly, List<Type>> Types { get; set; } = [];

    public AutoDiscoveryOptions For<T>() => For(CallingAssembly, typeof(T));
    public AutoDiscoveryOptions For(Type type) => For(CallingAssembly, type);
    public AutoDiscoveryOptions For(Assembly assembly, Type type)
    {
        if (!Types.ContainsKey(assembly))
            Types[assembly] = [type];
        else
            Types[assembly].Add(type);
       return this;
    }
}