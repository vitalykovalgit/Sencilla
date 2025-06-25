
namespace Sencilla.Messaging;

/// <summary>
/// 
/// </summary>
public interface IMessageStreamBroker
{
    IEnumerable<string> Names { get; }
    IMessageStream GetStream(string name);
    IMessageStream CreateStream(string name);
    IMessageStream GetOrCreateStream(string name);
    void RemoveStream(string name);
}

// <summary>
// Represents a message broker that manages message streams (queues or topics).
// </summary>
public interface IMessageBroker
{
    IMessageStreamBroker Queues { get; }
    IMessageStreamBroker Topics { get; }

    // IEnumerable<string> QueueNames { get; }
    // IMessageStream GetQueue(string name);
    // IMessageStream CreateQueue(string name);
    // IMessageStream GetOrCreateQueue(string name);
    // void RemoveQueue(string name);

    // IEnumerable<string> TopicNames { get; }
    // IMessageStream GetTopic(string name);
    // IMessageStream CreateTopic(string name);
    // IMessageStream GetOrCreateTopic(string name);
    // void RemoveTopic(string name);

}