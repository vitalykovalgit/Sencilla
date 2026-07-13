using System.Collections.Concurrent;

namespace Sencilla.Component.Security;

/// <summary>
/// Resolves and caches [SecureWith] chains — walks an entity's declarations to the
/// terminal role table, validating the shape on the way. Chains are pure type
/// metadata (schema, not policy), so resolution is attribute-driven and cached
/// per entity type for the process lifetime.
///
/// Usage:
/// <code>
/// var chain = SecureWithChains.TryResolve(typeof(ProjectItem));   // null when the type
/// if (chain == null) return;                                      // declares no [SecureWith]
/// // chain.Hops = [(ProjectItem, ProjectId)], chain.LinkType = ProjectUser
///
/// SecureWithChains.Validate(typeof(ProjectItem));                 // throws on misdeclaration —
///                                                                 // used by startup validation
/// </code>
/// Malformed declarations (hop without Via, terminal with Via, missing FK property,
/// key-type mismatch, non-terminating or cyclic chain) throw
/// <see cref="InvalidOperationException"/> with the exact defect named.
/// </summary>
public static class SecureWithChains
{
    private static readonly ConcurrentDictionary<Type, SecureWithChain?> Cache = new();

    /// <summary>
    /// The resolved chain of an entity, or null when the type has no [SecureWith]
    /// declaration (matrix rows then activate by global role holding only).
    /// Throws on a malformed declaration — a broken security path must never
    /// silently degrade to "no per-row security".
    /// </summary>
    public static SecureWithChain? TryResolve(Type entity) =>
        Cache.GetOrAdd(entity, Resolve);

    /// <summary>
    /// Eagerly validates an entity's chain (no-op for undeclared types). Called per
    /// type at startup so misdeclarations fail the deploy, not the first request.
    /// </summary>
    public static void Validate(Type entity) => TryResolve(entity);

    private static SecureWithChain? Resolve(Type entity)
    {
        if (entity.GetCustomAttribute<SecureWithAttribute>() == null)
            return null;

        var hops = new List<(Type Entity, PropertyInfo Via)>();
        var visited = new HashSet<Type> { entity };
        var current = entity;

        while (true)
        {
            var attr = current.GetCustomAttribute<SecureWithAttribute>()
                ?? throw new InvalidOperationException(
                    $"[SecureWith] chain of {entity.Name} does not terminate: {current.Name} has no [SecureWith] declaration.");

            var terminalKey = RoleableKeyOf(attr.Source);
            if (terminalKey != null)
            {
                // Terminal mode: Source is the role table of `current`.
                if (attr.Via != null)
                    throw new InvalidOperationException(
                        $"[SecureWith] on {current.Name}: Via is forbidden for a terminal declaration (source {attr.Source.Name} is a role table).");

                // An entity cannot be its own role table (terminal Source == current):
                // creating one row would provision another, recursively forever.
                if (attr.Source == current)
                    throw new InvalidOperationException(
                        $"[SecureWith] on {current.Name}: an entity cannot be its own role table. A role table secures itself in HOP mode via the entity it protects (e.g. [SecureWith(typeof(Project), Via = nameof({current.Name}.EntityId))]).");

                var entityKey = KeyOf(current)
                    ?? throw new InvalidOperationException(
                        $"[SecureWith] terminal entity {current.Name} must implement IEntity<TKey>.");

                if (terminalKey != entityKey)
                    throw new InvalidOperationException(
                        $"Role table {attr.Source.Name} implements IEntityRoleable<{terminalKey.Name}> but {current.Name} is keyed by {entityKey.Name}.");

                var linkKey = KeyOf(attr.Source)
                    ?? throw new InvalidOperationException(
                        $"Role table {attr.Source.Name} must implement IEntity<TKey>.");

                // Claims-derived roles are global-only; provisioning one as a per-row
                // creator role would be rejected by RoleableValidationHandler at every
                // create, rolling the whole operation back. Catch it at the deploy.
                if (attr.CreatorRole is (int)RoleType.Root or (int)RoleType.Anonymous or (int)RoleType.User)
                    throw new InvalidOperationException(
                        $"[SecureWith] on {current.Name}: CreatorRole {attr.CreatorRole} is a claims-derived (global-only) role and cannot be held on a row. Use Owner/Editor/Viewer or a custom role.");

                // The properties SecureWithFilter dereferences at query time must be
                // public (it cannot read explicit interface implementations). Verify at
                // the deploy rather than 500 on the first held-role request.
                RequirePublicProperties(attr.Source, "role table", "UserId", "RoleId", "EntityId");
                RequirePublicProperties(current, "terminal entity", "Id");
                foreach (var (hopEntity, _) in hops)
                    RequirePublicProperties(hopEntity, "hop entity", "Id");

                return new SecureWithChain
                {
                    Entity = entity,
                    Terminal = current,
                    LinkType = attr.Source,
                    LinkKeyType = linkKey,
                    TerminalKeyType = terminalKey,
                    CreatorRole = attr.CreatorRole,
                    Hops = hops,
                };
            }

            // Hop mode: Source is the parent entity; Via is the FK on `current`.
            if (string.IsNullOrWhiteSpace(attr.Via))
                throw new InvalidOperationException(
                    $"[SecureWith] on {current.Name}: Via is required when the source ({attr.Source.Name}) is another entity, e.g. Via = nameof({current.Name}.{attr.Source.Name}Id).");

            var via = current.GetProperty(attr.Via, BindingFlags.Public | BindingFlags.Instance)
                ?? throw new InvalidOperationException(
                    $"[SecureWith] on {current.Name}: Via property '{attr.Via}' does not exist.");

            var sourceKey = KeyOf(attr.Source)
                ?? throw new InvalidOperationException(
                    $"[SecureWith] on {current.Name}: hop source {attr.Source.Name} must implement IEntity<TKey>.");

            if (StripNullable(via.PropertyType) != StripNullable(sourceKey))
                throw new InvalidOperationException(
                    $"[SecureWith] on {current.Name}: Via property '{attr.Via}' is {via.PropertyType.Name} but {attr.Source.Name} is keyed by {sourceKey.Name}.");

            hops.Add((current, via));

            if (!visited.Add(attr.Source))
                throw new InvalidOperationException(
                    $"[SecureWith] chain of {entity.Name} is cyclic at {attr.Source.Name}.");

            current = attr.Source;
        }
    }

    /// <summary>TEntityKey of IEntityRoleable&lt;TEntityKey&gt;, or null when the type is not a role table.</summary>
    private static Type? RoleableKeyOf(Type type) => type
        .GetInterfaces()
        .FirstOrDefault(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IEntityRoleable<>))
        ?.GetGenericArguments()[0];

    /// <summary>TKey of IEntity&lt;TKey&gt;, or null.</summary>
    internal static Type? KeyOf(Type type) => type
        .GetInterfaces()
        .FirstOrDefault(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IEntity<>))
        ?.GetGenericArguments()[0];

    private static Type StripNullable(Type type) => Nullable.GetUnderlyingType(type) ?? type;

    private static void RequirePublicProperties(Type type, string role, params string[] names)
    {
        foreach (var name in names)
            if (type.GetProperty(name, BindingFlags.Public | BindingFlags.Instance) == null)
                throw new InvalidOperationException(
                    $"[SecureWith] {role} {type.Name} must expose a public '{name}' property (explicit interface implementations are not supported).");
    }
}
