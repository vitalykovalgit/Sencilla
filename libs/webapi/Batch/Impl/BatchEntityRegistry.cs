namespace Sencilla.Web.Batch;

/// <summary>
/// Scans loaded assemblies once for <c>[SencillaEntity]</c> types and builds a
/// descriptor (with a typed invoker) per entity.
/// </summary>
internal sealed class BatchEntityRegistry : IBatchEntityRegistry
{
    private readonly Dictionary<string, BatchEntityDescriptor> _byName;

    private BatchEntityRegistry(Dictionary<string, BatchEntityDescriptor> byName) => _byName = byName;

    public IReadOnlyCollection<BatchEntityDescriptor> All => _byName.Values;

    public bool TryGet(string entityName, out BatchEntityDescriptor descriptor)
    {
        if (!string.IsNullOrEmpty(entityName))
            return _byName.TryGetValue(entityName, out descriptor!);
        descriptor = null!;
        return false;
    }

    /// <summary>
    /// Builds the registry from every loaded assembly. Resolved lazily (first request)
    /// so all entity assemblies are guaranteed loaded.
    /// </summary>
    public static BatchEntityRegistry BuildFromAppDomain()
    {
        var map = new Dictionary<string, BatchEntityDescriptor>(StringComparer.OrdinalIgnoreCase);

        foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
        {
            Type[] types;
            try { types = assembly.GetTypes(); }
            catch (ReflectionTypeLoadException ex) { types = ex.Types.Where(t => t is not null).ToArray()!; }

            foreach (var type in types)
            {
                var attr = type.GetCustomAttribute<SencillaEntityAttribute>();
                if (attr is null || attr.Batch == BatchOpFlags.None)
                    continue;

                var keyType = type.GetInterfaces()
                    .FirstOrDefault(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IEntity<>))
                    ?.GetGenericArguments()[0];

                if (keyType is null)
                    continue; // not an IEntity<> — skip

                var invokerType = typeof(BatchEntityInvoker<,>).MakeGenericType(type, keyType);
                var invoker = (IBatchEntityInvoker)Activator.CreateInstance(invokerType)!;

                map[attr.Name] = new BatchEntityDescriptor
                {
                    Name = attr.Name,
                    EntityType = type,
                    KeyType = keyType,
                    AllowedOps = attr.Batch,
                    Invoker = invoker,
                };
            }
        }

        return new BatchEntityRegistry(map);
    }
}
