
namespace Sencilla.Component.Security;

/// <summary>
/// 
/// </summary>
public class SecurityAttributeDiscoverer: ITypeRegistrator
{
    /// <summary>
    /// Contains list of all permissions defined on entities in attributes
    /// </summary>
    public List<Matrix> Permissions { get; } = new List<Matrix>();

    /// <summary>
    /// Collect all permissions from attributes 
    /// </summary>
    /// <param name="container"></param>
    /// <param name="type"></param>
    public void Register(IContainer container, Type type)
    {
        if (typeof(IBaseEntity).IsAssignableFrom(type) && type.IsClass && !type.IsAbstract)
        {
            var attributes = type.GetCustomAttributes(typeof(AllowAccessAttribute), true);
            foreach (AllowAccessAttribute a in attributes)
            {
                // get resource 
                Permissions.Add(new Matrix 
                {
                    Resource = SecurityProvider.ResourceName(type),
                    Action = (int)a.Action,
                    Constraint = a.Constraint, 
                    Role = a.Role,
                });   
            }
        }
    }
}




