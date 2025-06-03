
namespace Sencilla.Core;

/// <summary>
/// Register class in container as sigleton 
/// Has highest priority 
/// </summary>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
public class SingletonLifetimeAttribute : Attribute
{
}

