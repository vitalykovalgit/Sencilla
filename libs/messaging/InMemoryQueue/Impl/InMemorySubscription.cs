namespace Sencilla.Messaging.InMemoryQueue;

public class InMemorySubscription<T> : IDisposable
{
    private readonly Channel<T> channel;
    private readonly ChannelWriter<T> Writer;
    private readonly ChannelReader<T> Reader;
    private readonly CancellationTokenSource CancellationTokenSource = new();
    private bool Disposed;

    public string Name { get; }
    public event Action? OnDisposed;

    internal InMemorySubscription(string name, int capacity = -1)
    {
        Name = name;
        
        var options = new BoundedChannelOptions(capacity)
        {
            FullMode = BoundedChannelFullMode.DropOldest,
            SingleReader = false,
            SingleWriter = false
        };

        channel = capacity == -1 ? Channel.CreateUnbounded<T>() : Channel.CreateBounded<T>(options);
        Writer = channel.Writer;
        Reader = channel.Reader;
    }

    internal async Task DeliverMessageAsync(T message, CancellationToken cancellationToken = default)
    {
        if (Disposed || CancellationTokenSource.Token.IsCancellationRequested)
            return;

        using var combined = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, CancellationTokenSource.Token);
        await Writer.WriteAsync(message, combined.Token);
    }

    public async Task<T?> ReceiveAsync(CancellationToken cancellationToken = default)
    {   
        using var combined = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, CancellationTokenSource.Token);
        if (await Reader.WaitToReadAsync(combined.Token))
        {
            if (Reader.TryRead(out var item))
            {
                return item;
            }
        }

        return default;
    }

    public bool TryReceive(out T? item)
    {
        return Reader.TryRead(out item);
    }

    public void Dispose()
    {
        if (!Disposed)
        {
            CancellationTokenSource.Cancel();
            Writer.Complete();
            CancellationTokenSource.Dispose();
            OnDisposed?.Invoke();
            Disposed = true;
        }
    }
}