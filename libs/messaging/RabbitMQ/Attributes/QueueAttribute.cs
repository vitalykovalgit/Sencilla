namespace Sencilla.Messaging.RabbitMQ;

/// <summary>
/// Attribute to specify queue routing for messages
/// </summary>
[AttributeUsage(AttributeTargets.Class)]
public class QueueAttribute : Attribute
{
    /// <summary>
    /// Queue name
    /// </summary>
    public string Name { get; }

    /// <summary>
    /// Initialize queue attribute
    /// </summary>
    /// <param name="name">Queue name</param>
    public QueueAttribute(string name)
    {
        Name = name;
    }
}
