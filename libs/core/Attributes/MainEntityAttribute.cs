
namespace Sencilla.Core;

/// <summary>
/// Used for entity framework core repositories to create a subset of entity 
/// </summary>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
public class MainEntityAttribute : Attribute
{
    public MainEntityAttribute(Type type)
    {
        Type = type;
    }

    public Type Type { get; }
}
