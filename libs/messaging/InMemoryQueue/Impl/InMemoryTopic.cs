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

    /// <summary>
    /// Fan out to every subscription. A write that FAILS is contained to its own subscriber and reported
    /// through <see cref="Failures"/> — previously Task.WhenAll surfaced it to the publisher, so one
    /// broken subscriber turned every publish on the topic into an error.
    ///
    /// <para>A write that BLOCKS is not contained, and deliberately so: a bounded subscription uses
    /// BoundedChannelFullMode.Wait, and that backpressure is the documented behaviour
    /// (Subscribe_WithCapacity_RespectsLimit). A slow subscriber still slows the publisher; only a
    /// failing one is isolated.</para>
    /// </summary>
    public async Task Write<T>(Message<T>? message, CancellationToken cancellationToken = default)
    {
        var tasks = Subscriptions.Select(async entry =>
        {
            try
            {
                await entry.Value.Write(message, cancellationToken);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                Failures?.Invoke(entry.Key, ex);
            }
        });

        await Task.WhenAll(tasks);
    }

    /// <summary>Raised when one subscription could not be written to; the others were unaffected.</summary>
    public event Action<string, Exception>? Failures;

    /// <summary>Capacity &lt;= 0 (the default) creates an unbounded subscription.</summary>
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