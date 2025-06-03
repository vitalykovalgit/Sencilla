namespace Sencilla.Messaging.InMemoryQueue;

public class InMemorySubscription<T> : IDisposable
{
    private readonly Channel<T> _channel;
    private readonly ChannelWriter<T> _writer;
    private readonly ChannelReader<T> _reader;
    private readonly CancellationTokenSource _cancellationTokenSource;
    private bool _disposed;

    public string Name { get; }
    public event Action? OnDisposed;

    internal InMemorySubscription(string name, int capacity)
    {
        Name = name;
        
        var options = new BoundedChannelOptions(capacity)
        {
            FullMode = BoundedChannelFullMode.DropOldest,
            SingleReader = false,
            SingleWriter = false
        };

        _channel = Channel.CreateBounded<T>(options);
        _writer = _channel.Writer;
        _reader = _channel.Reader;
        _cancellationTokenSource = new CancellationTokenSource();
    }

    internal async Task DeliverMessageAsync(T message, CancellationToken cancellationToken = default)
    {
        if (_disposed || _cancellationTokenSource.Token.IsCancellationRequested)
            return;

        try
        {
            using var combined = CancellationTokenSource.CreateLinkedTokenSource(
                cancellationToken, _cancellationTokenSource.Token);
            
            await _writer.WriteAsync(message, combined.Token);
        }
        catch (OperationCanceledException)
        {
            // Expected when subscription is disposed or cancelled
        }
        catch (InvalidOperationException)
        {
            // Channel is completed
        }
    }

    public async Task<T?> ReceiveAsync(CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();
        
        using var combined = CancellationTokenSource.CreateLinkedTokenSource(
            cancellationToken, _cancellationTokenSource.Token);

        if (await _reader.WaitToReadAsync(combined.Token))
        {
            if (_reader.TryRead(out var item))
            {
                return item;
            }
        }

        return default;
    }

    public bool TryReceive(out T? item)
    {
        ThrowIfDisposed();
        return _reader.TryRead(out item);
    }

    private void ThrowIfDisposed()
    {
        if (_disposed)
            throw new ObjectDisposedException(nameof(InMemorySubscription<T>));
    }

    public void Dispose()
    {
        if (!_disposed)
        {
            _cancellationTokenSource.Cancel();
            _writer.Complete();
            _cancellationTokenSource.Dispose();
            OnDisposed?.Invoke();
            _disposed = true;
        }
    }
}