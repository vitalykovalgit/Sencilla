namespace Sencilla.Messaging.EntityFramework.Tests;

/// <summary>
/// The durable transport's dispatcher hook: a routed message becomes a row through the scoped
/// queue and, like every transport, passes on; anything else passes through untouched.
/// </summary>
public class EfQueueMiddlewareTests
{
    [Stream("tasks")] public class QueuedByAttribute;
    public class QueuedByRoute;
    public class NotQueued;

    private class RecordingQueue : IMessageQueue
    {
        public List<(Message Message, string Stream)> Enqueued { get; } = [];
        public Task<Guid> Enqueue<T>(Message<T> message, string stream, DateTime? availableAt = null, CancellationToken token = default)
        {
            Enqueued.Add((message, stream));
            return Task.FromResult(message.Id);
        }
        public Task<Guid> Enqueue<T>(T payload, string stream, CancellationToken token = default) => Enqueue(new Message<T> { Payload = payload }, stream, null, token);
    }

    private static (EfQueueMiddleware Middleware, RecordingQueue Queue) Build()
    {
        var config = new EfMessagingProviderConfig();
        config.Streams.AddQueue("tasks");
        config.Routes.SendToStream("tasks", typeof(QueuedByRoute));
        var queue = new RecordingQueue();
        return (new EfQueueMiddleware(config, queue), queue);
    }

    [Fact]
    public async Task AttributeRouted_IsEnqueued_AndTheChainContinues()
    {
        var (middleware, queue) = Build();
        var continued = false;

        await middleware.HandleAsync(new Message<QueuedByAttribute> { Payload = new() }, (_, _) => { continued = true; return Task.CompletedTask; });

        var row = Assert.Single(queue.Enqueued);
        Assert.Equal("tasks", row.Stream);
        Assert.True(continued);
    }

    [Fact]
    public async Task FluentRouted_IsEnqueued()
    {
        var (middleware, queue) = Build();

        await middleware.HandleAsync(new Message<QueuedByRoute> { Payload = new() }, (_, _) => Task.CompletedTask);

        Assert.Equal("tasks", Assert.Single(queue.Enqueued).Stream);
    }

    [Fact]
    public async Task Unrouted_PassesThrough()
    {
        var (middleware, queue) = Build();
        var continued = false;

        await middleware.HandleAsync(new Message<NotQueued> { Payload = new() }, (_, _) => { continued = true; return Task.CompletedTask; });

        Assert.Empty(queue.Enqueued);
        Assert.True(continued);
    }
}
