namespace Sencilla.Messaging;

/// <summary>
/// Marks a command class to generate an extension method for IMessageDispatcher.
/// </summary>
[AttributeUsage(AttributeTargets.Class)]
public class ExtendDispatcherAttribute : Attribute
{
    /// <summary>
    /// The name of the extension method. If not provided, the class name will be used.
    /// </summary>
    public string? Method { get; set; }
}
