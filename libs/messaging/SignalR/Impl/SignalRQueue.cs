namespace Sencilla.Messaging.SignalR;

public class SignalRQueue : IMessageStream, IDisposable
{
    private readonly Channel<string> channel;
    private readonly ChannelWriter<string> Writer;
    private readonly ChannelReader<string> Reader;
    private readonly CancellationTokenSource CancellationTokenSource = new();
    private readonly IHubContext<MessagingHub> hubContext;
    private bool Disposed = false;
    public event Action? OnDisposed;

    public string Name { get; }

    public SignalRQueue(string name, IHubContext<MessagingHub> hubContext)
    {
        Name = name;
        this.hubContext = hubContext;
        channel = Channel.CreateUnbounded<string>();
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
        var json = JsonSerializer.Serialize(message);
        
        // Write to internal queue
        await Writer.WriteAsync(json, combined.Token);
        
        // Broadcast to SignalR clients
        try
        {
            await hubContext.Clients.All.SendAsync("ReceiveMessage", Name, json, combined.Token);
        }
        catch
        {
            // Ignore SignalR broadcast errors
        }
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