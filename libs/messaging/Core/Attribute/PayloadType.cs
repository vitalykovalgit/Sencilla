namespace Sencilla.Messaging;

/// <summary>
/// Attribute to specify topic routing for messages
/// </summary>
[AttributeUsage(AttributeTargets.Class)]
public class PayloadTypeAttribute : Attribute
{
    /// <summary>
    /// Exchange name
    /// </summary>
    public string Name { get; }

    public PayloadTypeAttribute(string name)
    {   
        Name = name;
    }
}
