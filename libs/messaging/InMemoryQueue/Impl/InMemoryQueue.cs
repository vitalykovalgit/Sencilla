namespace Sencilla.Messaging.InMemoryQueue;

public class InMemoryQueue: IMessageStream, IDisposable
{
    private readonly Channel<string> channel;
    private readonly ChannelWriter<string> Writer;
    private readonly ChannelReader<string> Reader;
    private readonly CancellationTokenSource CancellationTokenSource = new();
    private bool Disposed = false;
    public event Action? OnDisposed;

    public string Name { get; }

    public InMemoryQueue(string name, int? capacity = null)
    {
        Name = name;
        channel = capacity == null
            ? Channel.CreateUnbounded<string>()
            : Channel.CreateBounded<string>(new BoundedChannelOptions(capacity ?? -1) {
                FullMode = BoundedChannelFullMode.Wait,
                SingleReader = false,
                SingleWriter = false
            });

        Writer = channel.Writer;
        Reader = channel.Reader;
    }

    public Task Write<T>(T? payload, CancellationToken cancellationToken = default)
    {
        if (payload is null) return Task.CompletedTask;

        return Write(new Message<T> { Payload = payload }, cancellationToken);
    }

    public async Task Write<T>(Message<T>? message, CancellationToken cancellationToken = default)
    {
        if (message is null || CancellationTokenSource.Token.IsCancellationRequested) return;

        using var combined = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, CancellationTokenSource.Token);
        await Writer.WriteAsync(JsonSerializer.Serialize(message), combined.Token);
    }

    public async Task<string?> Read(CancellationToken cancellationToken = default)
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

    public async Task<Message<T>?> Read<T>(CancellationToken cancellationToken = default)
    {
        var json = await Read(cancellationToken);
        if (json is null) return default;

        return JsonSerializer.Deserialize<Message<T>>(json);
    }

    public void Dispose()
    {
        if (!Disposed)
        {
            CancellationTokenSource.Cancel();
            Writer.Complete();
            CancellationTokenSource.Dispose();
            Disposed = true;
            OnDisposed?.Invoke();
        }
    }    
}