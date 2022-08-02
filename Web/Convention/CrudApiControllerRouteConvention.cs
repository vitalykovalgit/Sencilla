using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ApplicationModels;
using System.Reflection;


namespace Sencilla.Web
{
    public class CrudApiControllerRouteConvention : IControllerModelConvention
    {
        public void Apply(ControllerModel controller)
        {
            var ctrlType = controller.ControllerType;
            if (ctrlType.IsGenericType && typeof(CrudApiController<,>) == ctrlType.GetGenericTypeDefinition())
            {
                var entityType = ctrlType.GenericTypeArguments[0];
                var crudApiAttr = entityType.GetCustomAttribute<CrudApiAttribute>();
                if (crudApiAttr?.Route != null)
                {
                    // set name 
                    controller.ControllerName = entityType.Name;

                    // add attribute 
                    controller.Selectors.Clear();
                    controller.Selectors.Add(new SelectorModel
                    {
                        AttributeRouteModel = new AttributeRouteModel(new RouteAttribute(crudApiAttr.Route))
                    });
                }
            }
        }
    }
}
