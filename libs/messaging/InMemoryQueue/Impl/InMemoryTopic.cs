namespace Sencilla.Messaging.InMemoryQueue;

public class InMemoryTopic(string name): IMessageStream, IDisposable
{
    private readonly ConcurrentDictionary<string, InMemoryQueue> Subscriptions = new();

    public string Name { get; } = name;

    private bool _disposed;

    public Task<string?> Read(CancellationToken cancellationToken = default)
    {
        throw new NotSupportedException("Reading from a topic is not supported. Use Subscribe to create a subscription and read from it.");
    }

    public Task<Message<T>?> Read<T>(CancellationToken cancellationToken = default)
    {
        throw new NotSupportedException("Reading from a topic is not supported. Use Subscribe to create a subscription and read from it.");
    }

    public async Task Write<T>(Message<T>? message, CancellationToken cancellationToken = default)
    {
        var tasks = Subscriptions.Values.Select(sub => sub.Write(message, cancellationToken));
        await Task.WhenAll(tasks);
    }

    public InMemoryQueue Subscribe(string subscriptionName, int capacity = -1)
    {
        var subscription = new InMemoryQueue(subscriptionName, capacity);
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