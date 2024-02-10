
namespace Sencilla.Repository.EntityFramework.Attributes;

[AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = true)]
public class DynamicContextAttribute : Attribute
{
    public Type ContextType { get; }

    public DynamicContextAttribute(Type contextType)
    {
        ContextType = contextType;
    }
}
