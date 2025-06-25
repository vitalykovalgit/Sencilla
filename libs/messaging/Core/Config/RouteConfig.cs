namespace Sencilla.Messaging;

public class RouteConfig
{
    /// <summary>
    /// Type of the entity that this route is for.
    /// This is used to determine the message type and how it should be processed.
    /// </summary>
    public required Type EntityType { get; init; }

    /// <summary>
    /// Message stream where to send messages of this type.
    /// </summary>
    public List<string> Streams { get; } = [];

    public void ToQueue(string queue)
    { 
        Streams.Add(queue);
    }
}