namespace Sencilla.Messaging.InMemoryQueue;

public class InMemoryMessageBroker : IDisposable
{
    private readonly ConcurrentDictionary<string, object> _queues;
    private readonly ConcurrentDictionary<string, object> _topics;

    //private readonly object _lock = new();
    private bool _disposed;

    public InMemoryMessageBroker()
    {
        _queues = new ConcurrentDictionary<string, object>();
        _topics = new ConcurrentDictionary<string, object>();
    }

    public InMemoryQueue<T> GetOrCreateQueue<T>(string queueName, int capacity = 1000)
    {
        ThrowIfDisposed();
        
        return (InMemoryQueue<T>)_queues.GetOrAdd(queueName, _ => new InMemoryQueue<T>(capacity));
    }

    public InMemoryTopic<T> GetOrCreateTopic<T>(string topicName)
    {
        ThrowIfDisposed();
        
        return (InMemoryTopic<T>)_topics.GetOrAdd(topicName, _ => new InMemoryTopic<T>());
    }

    public bool RemoveQueue(string queueName)
    {
        ThrowIfDisposed();
        
        if (_queues.TryRemove(queueName, out var queue))
        {
            if (queue is IDisposable disposable)
                disposable.Dispose();
            return true;
        }
        
        return false;
    }

    public bool RemoveTopic(string topicName)
    {
        ThrowIfDisposed();
        
        if (_topics.TryRemove(topicName, out var topic))
        {
            if (topic is IDisposable disposable)
                disposable.Dispose();
            return true;
        }
        
        return false;
    }

    public IReadOnlyList<string> GetQueueNames()
    {
        ThrowIfDisposed();
        return _queues.Keys.ToList();
    }

    public IReadOnlyList<string> GetTopicNames()
    {
        ThrowIfDisposed();
        return _topics.Keys.ToList();
    }

    private void ThrowIfDisposed()
    {
        if (_disposed)
            throw new ObjectDisposedException(nameof(InMemoryMessageBroker));
    }

    public void Dispose()
    {
        if (!_disposed)
        {
            foreach (var queue in _queues.Values.OfType<IDisposable>())
            {
                queue.Dispose();
            }
            
            foreach (var topic in _topics.Values.OfType<IDisposable>())
            {
                topic.Dispose();
            }
            
            _queues.Clear();
            _topics.Clear();
            _disposed = true;
        }
    }
}