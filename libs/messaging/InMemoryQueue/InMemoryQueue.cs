namespace Sencilla.Messaging.InMemoryQueue;

public class InMemoryQueue<T> : IDisposable
{
    private readonly Channel<T> _channel;
    private readonly ChannelWriter<T> _writer;
    private readonly ChannelReader<T> _reader;
    private readonly CancellationTokenSource _cancellationTokenSource;
    private bool _disposed;

    public InMemoryQueue(int capacity = 1000)
    {
        var options = new BoundedChannelOptions(capacity)
        {
            FullMode = BoundedChannelFullMode.Wait,
            SingleReader = false,
            SingleWriter = false
        };

        _channel = Channel.CreateBounded<T>(options);
        _writer = _channel.Writer;
        _reader = _channel.Reader;
        _cancellationTokenSource = new CancellationTokenSource();
    }

    public async Task EnqueueAsync(T item, CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();
        using var combined = CancellationTokenSource.CreateLinkedTokenSource(
            cancellationToken, _cancellationTokenSource.Token);
        
        await _writer.WriteAsync(item, combined.Token);
    }

    public async Task<T?> DequeueAsync(CancellationToken cancellationToken = default)
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

    public bool TryDequeue(out T? item)
    {
        ThrowIfDisposed();
        return _reader.TryRead(out item);
    }

    public void Complete()
    {
        _writer.Complete();
    }

    private void ThrowIfDisposed()
    {
        if (_disposed)
            throw new ObjectDisposedException(nameof(InMemoryQueue<T>));
    }

    public void Dispose()
    {
        if (!_disposed)
        {
            _cancellationTokenSource.Cancel();
            _writer.Complete();
            _cancellationTokenSource.Dispose();
            _disposed = true;
        }
    }
}