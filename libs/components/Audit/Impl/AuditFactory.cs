using System.Collections.Concurrent;

namespace Sencilla.Component.Audit;

/// <summary>
/// Pure audit-row builders (no I/O) — this is the unit-tested core. Reflects the entity's scalar columns
/// (value types + string, excluding [NotMapped] and navigations) to snapshot inserts/deletes and diff updates.
/// </summary>
public static class AuditFactory
{
    // camelCase both the field names (dictionary keys) and the old/new members, for the JS audit-view contract.
    static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        DictionaryKeyPolicy = JsonNamingPolicy.CamelCase,
    };

    public static Audit Insert(object entity, IAuditContext ctx)
        => New(entity, AuditAction.Insert, Snapshot(entity, valueIsNew: true), ctx);

    public static Audit Delete(object entity, IAuditContext ctx)
        => New(entity, AuditAction.Delete, Snapshot(entity, valueIsNew: false), ctx);

    /// <summary>Update row for the changed scalar columns; null when nothing changed (no-op updates are not audited).</summary>
    public static Audit? Update(object oldEntity, object newEntity, IAuditContext ctx)
    {
        var changes = Diff(oldEntity, newEntity);
        return changes.Count == 0 ? null : New(newEntity, AuditAction.Update, changes, ctx);
    }

    /// <summary>The entity's Id, stringified — the type-agnostic <see cref="Audit.EntityId"/> and the update match key.</summary>
    public static string IdOf(object entity)
        => entity.GetType().GetProperty("Id")?.GetValue(entity)?.ToString() ?? "";

    static Audit New(object entity, AuditAction action, Dictionary<string, object?> changes, IAuditContext ctx) => new()
    {
        EntityType = entity.GetType().Name,
        EntityId = IdOf(entity),
        Action = action,
        ActorType = ctx.ActorType,
        ActorId = ctx.ActorId,
        ImpersonatedById = ctx.ImpersonatedById,
        Reason = ctx.Reason,
        CorrelationId = ctx.CorrelationId,
        Changes = JsonSerializer.Serialize(changes, JsonOptions),
    };

    // Full-row snapshot: {field:{old,new}} with the value on the new side (insert) or old side (delete).
    static Dictionary<string, object?> Snapshot(object entity, bool valueIsNew)
    {
        var d = new Dictionary<string, object?>();
        foreach (var p in ScalarProps(entity.GetType()))
        {
            var v = p.GetValue(entity);
            d[p.Name] = valueIsNew ? new { old = (object?)null, @new = v } : new { old = v, @new = (object?)null };
        }
        return d;
    }

    // Bookkeeping stamps that change on every write — a diff that ONLY touches these is a semantic
    // no-op (e.g. re-writing an entity's status to the same value bumps UpdatedDate). Excluded from the
    // update diff so such no-ops produce no audit row. NOT excluded from insert/delete snapshots (there
    // the timestamp is part of the full-row record), and DeletedDate stays IN the diff (soft-delete is
    // a real change).
    static readonly HashSet<string> NoiseColumns = new(StringComparer.Ordinal) { "CreatedDate", "UpdatedDate" };

    static Dictionary<string, object?> Diff(object oldEntity, object newEntity)
    {
        var d = new Dictionary<string, object?>();
        foreach (var p in ScalarProps(oldEntity.GetType()))
        {
            if (NoiseColumns.Contains(p.Name))
                continue;

            var ov = p.GetValue(oldEntity);
            var nv = p.GetValue(newEntity);
            if (!Equals(ov, nv))
                d[p.Name] = new { old = ov, @new = nv };
        }
        return d;
    }

    // ponytail: computed once per entity type; key space is the (small, bounded) set of audited types, so no eviction.
    static readonly ConcurrentDictionary<Type, PropertyInfo[]> ScalarPropsCache = new();

    static PropertyInfo[] ScalarProps(Type type) => ScalarPropsCache.GetOrAdd(type, static t => t
        .GetProperties(BindingFlags.Public | BindingFlags.Instance)
        .Where(p => p.CanRead && p.CanWrite
            && (p.PropertyType.IsValueType || p.PropertyType == typeof(string))
            && p.GetCustomAttribute<NotMappedAttribute>() == null)
        .ToArray());
}
