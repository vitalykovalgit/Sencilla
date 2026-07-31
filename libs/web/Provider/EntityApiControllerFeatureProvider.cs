namespace Sencilla.Web;

/// <summary>
/// Materialises one controller per (entity, generic controller) pair: every type marked
/// <see cref="CrudApiAttribute"/> is closed over every open generic controller marked
/// <see cref="EntityApiAttribute"/> — <see cref="CrudApiController{TEntity,TKey}"/> itself, plus whatever the
/// referenced components contribute.
/// </summary>
public class EntityApiControllerFeatureProvider : IApplicationFeatureProvider<ControllerFeature>
{
    public void PopulateFeature(IEnumerable<ApplicationPart> parts, ControllerFeature feature)
    {
        // Scanning the AppDomain rather than `parts` is deliberate and long-standing: the entities live in the
        // host's assemblies and the generic controllers in Sencilla's, and neither needs to be an MVC part —
        // a type added to the feature directly is enough for MVC to route and activate it.
        var appTypes = AppDomain.CurrentDomain.GetAssemblies().SelectMany(a => a.ExportedTypes).ToList();

        var controllers = appTypes
            .Where(t => t.IsGenericTypeDefinition
                     && t.GetGenericArguments().Length == 2
                     && t.GetCustomAttributes(typeof(EntityApiAttribute), false).Any())
            .ToList();

        foreach (var entityType in appTypes)
        {
            if (!entityType.GetCustomAttributes<CrudApiAttribute>().Any())
                continue;

            var keyType = entityType
                .GetInterfaces()
                .FirstOrDefault(p => p.IsGenericType && p.GetGenericTypeDefinition() == typeof(IEntity<>))
                ?.GetGenericArguments()[0];

            if (keyType == null)
                continue;

            foreach (var controller in controllers)
            {
                // MakeGenericType is the only thing that evaluates the controller's constraints, so a
                // controller that doesn't fit this entity announces itself by throwing, and is skipped.
                try { feature.Controllers.Add(controller.MakeGenericType(entityType, keyType).GetTypeInfo()); }
                catch (ArgumentException) { }
            }
        }
    }
}
