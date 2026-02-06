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
    public void Register(IServiceCollection container, Type type) => container.AddSencillaSecurityFromAttributes(type, Permissions);
}




