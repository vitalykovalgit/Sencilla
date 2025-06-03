namespace Sencilla.Component.Security;

/// <summary>
/// Define custom resource for the entity
/// </summary>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
public class ResourceAttribute : Attribute
{
    /// <summary>
    /// 
    /// </summary>
    public ResourceAttribute(string name)
    {
        Name = name;
    }

    /// <summary>
    /// Resource name
    /// </summary>
    public string Name { get; }

}
