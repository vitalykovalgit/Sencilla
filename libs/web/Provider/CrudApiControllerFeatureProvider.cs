namespace Sencilla.Web;

public class CrudApiControllerFeatureProvider: IApplicationFeatureProvider<ControllerFeature>
{
    public void PopulateFeature(IEnumerable<ApplicationPart> parts, ControllerFeature feature)
    {
        var appTypes = AppDomain.CurrentDomain.GetAssemblies().SelectMany(a=>a.ExportedTypes);
        foreach (var entityType in appTypes)
        {
            if (entityType.GetCustomAttributes<CrudApiAttribute>().Any())
            {
                var keyType = entityType
                    .GetInterfaces()
                    .FirstOrDefault(p => p.IsGenericType && p.GetGenericTypeDefinition() == typeof(IEntity<>))
                    ?.GetGenericArguments()[0];

                if (keyType != null)
                {
                    var controllerType = typeof(CrudApiController<,>).MakeGenericType(entityType, keyType).GetTypeInfo();
                    feature.Controllers.Add(controllerType);
                }
            }
        }
    }
}
