using System.Text.Json;
using System.Text.Json.Serialization.Metadata;

namespace Sencilla.Web;

/// <summary>
/// EF's AddRange and UpdateRange walk the whole object graph and no event fires for what they find there, so a CRUD
/// body could write any row a navigation reaches, past every permission check: a customer's own project carrying
/// `user: { id: admin, email: mine }` rewrote the administrator's login, and their own user carrying
/// `roles: [{ roleId: admin }]` made them one.
///
/// So nothing below an entity binds from a request body: a relationship arrives as its foreign key, and each row is
/// written, and judged, on its own route. Responses still carry navigations (`?with=`), because only the setter goes.
///
/// ponytail: MVC's JSON options only, which every <see cref="CrudApiController{TEntity,TKey}"/> body binds through.
/// A minimal-API endpoint binds whatever type its host picks; give those options this too if one ever takes an entity.
/// </summary>
public static class RequestBodyNavigations
{
    public static JsonSerializerOptions IgnoreNavigationsInRequestBodies(this JsonSerializerOptions options)
    {
        options.TypeInfoResolver = (options.TypeInfoResolver ?? new DefaultJsonTypeInfoResolver()).WithAddedModifier(IgnoreNavigations);
        return options;
    }

    static void IgnoreNavigations(JsonTypeInfo type)
    {
        if (type.Kind != JsonTypeInfoKind.Object || !type.Type.IsAssignableTo(typeof(IBaseEntity)))
            return;

        foreach (var property in type.Properties.Where(p => IsNavigation(p.PropertyType)))
            property.Set = null;
    }

    static bool IsNavigation(Type type)
        => type.IsAssignableTo(typeof(IBaseEntity))
        || type.IsArray && type.GetElementType()!.IsAssignableTo(typeof(IBaseEntity))
        || type.IsGenericType && type.GetGenericArguments().Any(argument => argument.IsAssignableTo(typeof(IBaseEntity)));
}
