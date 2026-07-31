namespace Sencilla.Web;

/// <summary>
/// Routes every generic controller emitted by <see cref="EntityApiControllerFeatureProvider"/> under its
/// entity's <see cref="CrudApiAttribute"/> route, and applies the entity's authorization posture to it.
/// </summary>
public class EntityApiControllerRouteConvention : IControllerModelConvention
{
    public void Apply(ControllerModel controller)
    {
        var ctrlType = controller.ControllerType;
        if (!ctrlType.IsGenericType
            || !ctrlType.GetGenericTypeDefinition().GetCustomAttributes(typeof(EntityApiAttribute), false).Any())
            return;

        // Check if we need to add routing
        var entityType = ctrlType.GenericTypeArguments[0];
        var crudApiAttr = entityType.GetCustomAttribute<CrudApiAttribute>();
        if (crudApiAttr?.Route != null)
        {
            // Every generic controller for an entity shares the entity's name and route prefix: they are one
            // API surface split across classes, and an API explorer should keep showing them as one.
            controller.ControllerName = entityType.Name;
            controller.Selectors.Clear();
            controller.Selectors.Add(new SelectorModel
            {
                AttributeRouteModel = new AttributeRouteModel(new RouteAttribute(crudApiAttr.Route))
            });
        }

        // Check if we need to authorize controller
        var authorizeAttr = entityType.GetCustomAttribute<AuthorizeAttribute>();
        if (authorizeAttr != null)
            controller.Filters.Add(new AuthorizeFilter());

        // Check if we need to allow anonymous access
        var anonymousAttr = entityType.GetCustomAttribute<AllowAnonymousAttribute>();
        if (anonymousAttr != null)
            controller.Filters.Add(new AllowAnonymousFilter());
    }
}
