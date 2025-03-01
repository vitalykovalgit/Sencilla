namespace Sencilla.Web;

public class CrudApiControllerRouteConvention : IControllerModelConvention
{
    public void Apply(ControllerModel controller)
    {
        var ctrlType = controller.ControllerType;
        if (ctrlType.IsGenericType && typeof(CrudApiController<,>) == ctrlType.GetGenericTypeDefinition())
        {
            // Check if we need to add routing 
            var entityType = ctrlType.GenericTypeArguments[0];
            var crudApiAttr = entityType.GetCustomAttribute<CrudApiAttribute>();
            if (crudApiAttr?.Route != null)
            {
                // set name 
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
}
