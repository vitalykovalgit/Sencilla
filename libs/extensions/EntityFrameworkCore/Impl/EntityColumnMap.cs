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
    /// The convention mirrors EF Core's own: an integral key is an IDENTITY column, anything else
    /// (<c>string</c>, <c>Guid</c>) is client-supplied. An explicit
    /// <see cref="DatabaseGeneratedAttribute"/> always wins over the convention.
    ///
    /// This is why <c>Sencilla.Component.I18n</c>'s <c>Resource</c> — whose key IS the dotted
    /// translation key — could never be upserted: every attempt emitted an INSERT without [Id].
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

        var type = Nullable.GetUnderlyingType(key.PropertyType) ?? key.PropertyType;

        return type == typeof(int) || type == typeof(long) || type == typeof(short) || type == typeof(byte);
    }
}
