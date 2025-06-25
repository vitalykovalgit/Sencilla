namespace Sencilla.Messaging.InMemoryQueue;

public class InMemoryMessageBroker : IDisposable
{
    private readonly ConcurrentDictionary<string, object> Queues;
    private readonly ConcurrentDictionary<string, object> Topics;
    private bool Disposed;

    public InMemoryMessageBroker()
    {
        Queues = new ConcurrentDictionary<string, object>();
        Topics = new ConcurrentDictionary<string, object>();
    }

    public InMemoryQueue<T> GetOrCreateQueue<T>(string queueName, int capacity = -1)
    {   
        return (InMemoryQueue<T>)Queues.GetOrAdd(queueName, _ => new InMemoryQueue<T>(capacity));
    }

    public bool RemoveQueue(string queueName)
    {
        if (Queues.TryRemove(queueName, out var queue))
        {
            if (queue is IDisposable disposable)
                disposable.Dispose();
            return true;
        }
        
        return false;
    }
    
    public InMemoryTopic<T> GetOrCreateTopic<T>(string topicName)
    {
        return (InMemoryTopic<T>)Topics.GetOrAdd(topicName, _ => new InMemoryTopic<T>());
    }

    public bool RemoveTopic(string topicName)
    {
        if (Topics.TryRemove(topicName, out var topic))
        {
            if (topic is IDisposable disposable)
                disposable.Dispose();
            return true;
        }

        return false;
    }

    public IReadOnlyList<string> GetQueueNames()
    {
        return Queues.Keys.ToList();
    }

    public IReadOnlyList<string> GetTopicNames()
    {
        return Topics.Keys.ToList();
    }

    public void Dispose()
    {
        if (!Disposed)
        {
            foreach (var queue in Queues.Values.OfType<IDisposable>())
            {
                queue.Dispose();
            }
            
            foreach (var topic in Topics.Values.OfType<IDisposable>())
            {
                topic.Dispose();
            }
            
            Queues.Clear();
            Topics.Clear();
            Disposed = true;
        }
    }
}