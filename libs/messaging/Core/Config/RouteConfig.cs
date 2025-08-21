namespace Sencilla.Messaging;

/// <summary>
/// Represents the routing configuration for a specific message type.
/// </summary>
public class RouteConfig(Type entityType)
{
    /// <summary>
    /// Message stream where to send messages of this type.
    /// Let's use map to avoid duplicates 
    /// </summary>
    private readonly ConcurrentDictionary<string, string> StreamMap = new();

    /// <summary>
    /// List of all streams for this route.
    /// </summary>
    public IEnumerable<string> Streams => StreamMap.Values;

    /// <summary>
    /// Type of the entity that this route is for.
    /// This is used to determine the message type and how it should be processed.
    /// </summary>
    public Type EntityType { get; } = entityType;

    /// <summary>
    /// Adds a queue to the list of streams for this route.
    /// </summary>
    /// <param name="stream"></param>
    public void ToStream(params IEnumerable<string> streams)
    {
        streams.ForEach(stream => StreamMap[stream] = stream);
    }

    /// <summary>
    /// Adds multiple streams to the list of streams for this route.
    /// </summary>
    /// <param name="streams"></param>
    public void To(params IEnumerable<string> streams)
    {
        streams.ForEach(stream => StreamMap[stream] = stream);
    }
}