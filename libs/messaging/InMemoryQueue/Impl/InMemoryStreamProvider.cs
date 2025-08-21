
namespace Sencilla.Messaging.InMemoryQueue;

public class InMemoryStreamProvider(/*InMemoryProviderConfig providerConfig*/) : IMessageStreamProvider
{
    private readonly ConcurrentDictionary<string, IMessageStream> Streams = [];

    public IMessageStream GetOrCreateStream(StreamConfig config)
    {
        if (config?.Name == null)
            throw new ArgumentNullException(nameof(config.Name), "Stream name cannot be null.");
            
        return Streams.GetOrAdd(config.Name, _ => new InMemoryQueue(config.Name, config.MaxQueueSize));
    }

    public IMessageStream? GetStream(StreamConfig config)
    {
        return GetOrCreateStream(config);
    }
}

// public class InMemoryMessageBroker : IMessageBroker, IDisposable
// {
//     //private readonly ConcurrentDictionary<string, object> Topics;
//     private bool Disposed;

//     public InMemoryMessageBroker()
//     {
//         Streams = new ConcurrentDictionary<string, IMessageStream>();
//         //Topics = new ConcurrentDictionary<string, object>();
//     }

//     public Task PublishAsync<T>(Message<T> message)
//     {
//         //throw new NotImplementedException();
//     }

//     public InMemoryQueue<T> GetOrCreateQueue<T>(string queueName, int capacity = -1)
//     {
//         return (InMemoryQueue<T>)Queues.GetOrAdd(queueName, _ => new InMemoryQueue<T>(capacity));
//     }

//     public bool RemoveQueue(string queueName)
//     {
//         if (Queues.TryRemove(queueName, out var queue))
//         {
//             if (queue is IDisposable disposable)
//                 disposable.Dispose();
//             return true;
//         }

//         return false;
//     }

//     public InMemoryTopic<T> GetOrCreateTopic<T>(string topicName)
//     {
//         return (InMemoryTopic<T>)Topics.GetOrAdd(topicName, _ => new InMemoryTopic<T>());
//     }

//     public bool RemoveTopic(string topicName)
//     {
//         if (Topics.TryRemove(topicName, out var topic))
//         {
//             if (topic is IDisposable disposable)
//                 disposable.Dispose();
//             return true;
//         }

//         return false;
//     }

//     public void Dispose()
//     {
//         if (!Disposed)
//         {
//             foreach (var queue in Queues.Values.OfType<IDisposable>())
//             {
//                 queue.Dispose();
//             }

//             foreach (var topic in Topics.Values.OfType<IDisposable>())
//             {
//                 topic.Dispose();
//             }

//             Queues.Clear();
//             Topics.Clear();
//             Disposed = true;
//         }
//     }

// }