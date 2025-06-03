namespace Sencilla.Messaging.InMemoryQueue;

public class InMemoryTopic<T> : IDisposable
{
    private readonly ConcurrentDictionary<string, InMemorySubscription<T>> _subscriptions;

    //private readonly object _lock = new();
    private bool _disposed;

    public InMemoryTopic()
    {
        _subscriptions = new ConcurrentDictionary<string, InMemorySubscription<T>>();
    }

    public async Task PublishAsync(T message, CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();
        
        var subscriptions = _subscriptions.Values.ToList();
        var tasks = subscriptions.Select(sub => sub.DeliverMessageAsync(message, cancellationToken));
        
        await Task.WhenAll(tasks);
    }

    public InMemorySubscription<T> Subscribe(string subscriptionName, int capacity = 1000)
    {
        ThrowIfDisposed();
        
        var subscription = new InMemorySubscription<T>(subscriptionName, capacity);
        
        if (!_subscriptions.TryAdd(subscriptionName, subscription))
        {
            subscription.Dispose();
            throw new InvalidOperationException($"Subscription '{subscriptionName}' already exists.");
        }

        subscription.OnDisposed += () => _subscriptions.TryRemove(subscriptionName, out _);
        
        return subscription;
    }

    public bool Unsubscribe(string subscriptionName)
    {
        ThrowIfDisposed();
        
        if (_subscriptions.TryRemove(subscriptionName, out var subscription))
        {
            subscription.Dispose();
            return true;
        }
        
        return false;
    }

    public IReadOnlyList<string> GetSubscriptionNames()
    {
        ThrowIfDisposed();
        return _subscriptions.Keys.ToList();
    }

    private void ThrowIfDisposed()
    {
        if (_disposed)
            throw new ObjectDisposedException(nameof(InMemoryTopic<T>));
    }

    public void Dispose()
    {
        if (!_disposed)
        {
            foreach (var subscription in _subscriptions.Values)
            {
                subscription.Dispose();
            }
            _subscriptions.Clear();
            _disposed = true;
        }
    }
}