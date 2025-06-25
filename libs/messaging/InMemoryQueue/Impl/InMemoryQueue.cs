namespace Sencilla.Messaging.InMemoryQueue;

public class InMemoryQueue<T> : IDisposable
{
    private readonly Channel<T> channel;
    private readonly ChannelWriter<T> Writer;
    private readonly ChannelReader<T> Reader;
    private readonly CancellationTokenSource CancellationTokenSource = new();
    private bool Disposed = false;

    public InMemoryQueue(int capacity = -1)
    {
        var options = new BoundedChannelOptions(capacity)
        {
            FullMode = BoundedChannelFullMode.Wait,
            SingleReader = false,
            SingleWriter = false
        };

        channel = capacity == -1 ? Channel.CreateUnbounded<T>() : Channel.CreateBounded<T>(options);
        Writer = channel.Writer;
        Reader = channel.Reader;
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="item"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    public async Task EnqueueAsync(T? item, CancellationToken cancellationToken = default)
    {
        if (item is null) return;

        using var combined = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, CancellationTokenSource.Token);
        await Writer.WriteAsync(item, combined.Token);
    }

    public async Task<T?> DequeueAsync(CancellationToken cancellationToken = default)
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

    public bool TryDequeue(out T? item)
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
            Disposed = true;
        }
    }
}