
namespace Sencilla.Core;

/// <summary>
/// Register all interfaces for type from attribute 
/// NOTE: This class will be automatically registered in container
/// </summary>
public class ImplementAttributeRegistrator : ITypeRegistrator
{
    public void Register(IServiceCollection container, Type type)
    {
        var attributes = type.GetCustomAttributes(typeof(ImplementAttribute), true);
        foreach (ImplementAttribute attribute in attributes)
        {
            if (attribute.PerRequest)
                container.AddScoped(attribute.Interface, type);
            else
                container.AddTransient(attribute.Interface, type);
        }
    }
}
