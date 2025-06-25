namespace Sencilla.Messaging.InMemoryQueue;

public class InMemoryTopic<T> : IDisposable
{
    private readonly ConcurrentDictionary<string, InMemorySubscription<T>> Subscriptions;

    //private readonly object _lock = new();
    private bool _disposed;

    public InMemoryTopic()
    {
        Subscriptions = new ConcurrentDictionary<string, InMemorySubscription<T>>();
    }

    public async Task PublishAsync(T message, CancellationToken cancellationToken = default)
    {
        var subscriptions = Subscriptions.Values.ToList();
        var tasks = subscriptions.Select(sub => sub.DeliverMessageAsync(message, cancellationToken));
        
        await Task.WhenAll(tasks);
    }

    public InMemorySubscription<T> Subscribe(string subscriptionName, int capacity = -1)
    {
        var subscription = new InMemorySubscription<T>(subscriptionName, capacity);
        if (!Subscriptions.TryAdd(subscriptionName, subscription))
        {
            subscription.Dispose();
            throw new InvalidOperationException($"Subscription '{subscriptionName}' already exists.");
        }

        subscription.OnDisposed += () => Subscriptions.TryRemove(subscriptionName, out _);
        
        return subscription;
    }

    public bool Unsubscribe(string subscriptionName)
    {
        if (Subscriptions.TryRemove(subscriptionName, out var subscription))
        {
            subscription.Dispose();
            return true;
        }
        
        return false;
    }

    public IReadOnlyList<string> GetSubscriptionNames()
    {
        return Subscriptions.Keys.ToList();
    }

    public void Dispose()
    {
        if (!_disposed)
        {
            foreach (var subscription in Subscriptions.Values)
            {
                subscription.Dispose();
            }
            Subscriptions.Clear();
            _disposed = true;
        }
    }
}