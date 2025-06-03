namespace Sencilla.Messaging.RabbitMQ;

/// <summary>
/// Attribute to specify topic routing for messages
/// </summary>
[AttributeUsage(AttributeTargets.Class)]
public class TopicAttribute : Attribute
{
    /// <summary>
    /// Exchange name
    /// </summary>
    public string Exchange { get; }

    /// <summary>
    /// Routing key
    /// </summary>
    public string RoutingKey { get; }

    /// <summary>
    /// Initialize topic attribute
    /// </summary>
    /// <param name="exchange">Exchange name</param>
    /// <param name="routingKey">Routing key</param>
    public TopicAttribute(string exchange, string routingKey)
    {
        Exchange = exchange;
        RoutingKey = routingKey;
    }
}
