namespace Sencilla.Messaging;

/// <summary>
/// Attribute to specify topic routing for messages
/// </summary>
[AttributeUsage(AttributeTargets.Class)]
public class PayloadTypeAttribute(string name) : Attribute
{
    /// <summary>
    /// Exchange name
    /// </summary>
    public string Name { get; } = name;
}
