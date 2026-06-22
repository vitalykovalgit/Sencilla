namespace Sencilla.Repository.EntityFramework;

/// <summary>
/// Resolves and caches the <see cref="BusinessKeyAttribute"/> properties of an append-only entity and
/// builds an equality predicate matching a target instance's key values — used to locate the current
/// open version to supersede on write.
/// </summary>
internal static class BusinessKeys
{
    static readonly ConcurrentDictionary<Type, PropertyInfo[]> Cache = new();

    public static PropertyInfo[] Resolve(Type type) => Cache.GetOrAdd(type, static t =>
    {
        var attr = t.GetCustomAttribute<BusinessKeyAttribute>()
            ?? throw new InvalidOperationException(
                $"{t.Name} is {nameof(IEntityAppendOnlyTrack)} but has no [{nameof(BusinessKeyAttribute)}]. " +
                $"Declare the business key, e.g. [BusinessKey(nameof(From), nameof(To))].");

        return attr.Keys
            .Select(name => t.GetProperty(name)
                ?? throw new InvalidOperationException($"[{nameof(BusinessKeyAttribute)}] on {t.Name} names unknown property '{name}'."))
            .ToArray();
    });

    /// <summary>Predicate <c>x => x.k1 == target.k1 &amp;&amp; …</c> over the entity's business key.</summary>
    public static Expression<Func<T, bool>> Match<T>(T target)
    {
        var keyProps = Resolve(typeof(T));
        var x = Expression.Parameter(typeof(T), "x");

        Expression? body = null;
        foreach (var p in keyProps)
        {
            var equals = Expression.Equal(Expression.Property(x, p), Expression.Constant(p.GetValue(target), p.PropertyType));
            body = body == null ? equals : Expression.AndAlso(body, equals);
        }

        return Expression.Lambda<Func<T, bool>>(body ?? Expression.Constant(true), x);
    }
}
