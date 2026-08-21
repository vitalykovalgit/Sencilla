using Sencilla.Extensions.EntityFrameworkCore.Entity.Attributes;

namespace Sencilla.Repository.EntityFramework.Extension;

/// <summary>
/// Which of an entity's properties are real columns, shared by every hand-built SQL statement in
/// this assembly (MERGE for upsert, MERGE + SELECT for get-or-create).
///
/// It has to be one implementation: naming a property that has no column behind it makes SQL Server
/// reject the whole statement with «Invalid column name», so a builder that forgets one of these
/// rules is broken for every entity that trips it. <see cref="GetOrCreateQueryBuilder{TEntity}"/>
/// excluded collection navigations while <see cref="UpsertQueryBuilder{TEntity}"/> excluded only the
/// reference side, so upserting any entity with an <see cref="InversePropertyAttribute"/> collection
/// — Sencilla.Component.I18n's own <c>Resource.Translations</c> among them — always threw.
/// </summary>
internal static class EntityColumnMap
{
    /// <summary>
    /// Properties EF maps to columns: everything except <see cref="NotMappedAttribute"/>,
    /// <see cref="SkipUpsertAttribute"/> and navigations.
    /// </summary>
    public static List<PropertyInfo> MappedProperties(Type entityType) =>
        entityType.GetProperties().Where(IsColumn).ToList();

    /// <summary>True when the property is backed by a column rather than a navigation.</summary>
    public static bool IsColumn(PropertyInfo property) =>
        property.GetCustomAttribute<NotMappedAttribute>() is null
        && property.GetCustomAttribute<SkipUpsertAttribute>() is null
        && !IsNavigation(property.PropertyType);

    /// <summary>
    /// True for a navigation: a reference (<c>Translation.Resource</c>) or a collection
    /// (<c>Resource.Translations</c>). `string` and `byte[]` are not generic over an entity, so they
    /// fall through and stay columns.
    /// </summary>
    public static bool IsNavigation(Type type)
    {
        var underlying = Nullable.GetUnderlyingType(type) ?? type;

        if (underlying.IsAssignableTo(typeof(IBaseEntity)))
            return true;

        if (underlying.IsGenericType)
        {
            var elementType = underlying.GetGenericArguments().FirstOrDefault();
            if (elementType != null && elementType.IsAssignableTo(typeof(IBaseEntity)))
                return true;
        }

        return false;
    }

    /// <summary>
    /// True when the database produces the primary key itself, so a MERGE's INSERT must NOT name the
    /// key column. False when the caller supplies it, in which case omitting it inserts NULL and the
    /// statement dies on the NOT NULL constraint.
    ///
    /// Only a <c>string</c> key counts as client-supplied. An explicit
    /// <see cref="DatabaseGeneratedAttribute"/> still wins over that convention.
    ///
    /// This is why <c>Sencilla.Component.I18n</c>'s <c>Resource</c> — whose key IS the dotted
    /// translation key — could never be upserted: every attempt emitted an INSERT without [Id].
    ///
    /// A <c>Guid</c> key deliberately stays on the database-generated side even though the client
    /// usually does supply one. Guid PKs here are declared <c>DEFAULT NEWSEQUENTIALID()</c> precisely
    /// as "a fallback for non-EF inserts", and this raw MERGE *is* a non-EF insert: callers such as
    /// the clipart and palette-colour dialogs send an EMPTY id on create and rely on the default to
    /// allocate one. Naming [Id] would write all-zeros, and the next create would then MATCH that
    /// zeros row on the key and silently overwrite it instead of inserting. Widening this to Guid
    /// needs those callers changed first.
    /// </summary>
    public static bool IsDatabaseGeneratedKey(Type entityType)
    {
        var key = entityType.GetProperties()
            .FirstOrDefault(p => string.Equals(p.Name, "Id", StringComparison.OrdinalIgnoreCase));

        if (key is null)
            return true;

        var generated = key.GetCustomAttribute<DatabaseGeneratedAttribute>();
        if (generated is not null)
            return generated.DatabaseGeneratedOption != DatabaseGeneratedOption.None;

        return (Nullable.GetUnderlyingType(key.PropertyType) ?? key.PropertyType) != typeof(string);
    }
}
