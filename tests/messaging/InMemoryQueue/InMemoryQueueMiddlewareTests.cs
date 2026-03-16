namespace Sencilla.Messaging.InMemoryQueue.Tests;

public class InMemoryQueueMiddlewareTests
{
    [Fact]
    public async Task ProcessAsync_NullMessage_DoesNotThrow()
    {
        var config = new InMemoryProviderConfig();
        var provider = new InMemoryStreamProvider();

        var middleware = new InMemoryQueueMiddleware(config, provider);

        await middleware.ProcessAsync<string>(null);
    }

    [Fact]
    public async Task ProcessAsync_NoRoutes_DoesNotThrow()
    {
        var config = new InMemoryProviderConfig();
        var provider = new InMemoryStreamProvider();

        var middleware = new InMemoryQueueMiddleware(config, provider);
        var message = new Message<string> { Payload = "test" };

        await middleware.ProcessAsync(message);
    }

    [Fact]
    public async Task ProcessAsync_WithRoute_SendsToStream()
    {
        var config = new InMemoryProviderConfig();
        config.AddStreams(s => s.AddStream("my-queue"));
        config.AddRoutes(r => r.Send<string>().ToStream("my-queue"));
        var provider = new InMemoryStreamProvider();

        var middleware = new InMemoryQueueMiddleware(config, provider);
        var message = new Message<string> { Payload = "routed" };
        await middleware.ProcessAsync(message);

        var streamConfig = config.Streams.GetConfig("my-queue")!;
        var stream = provider.GetStream(streamConfig);
        var result = await stream!.Read<string>();

        Assert.Equal("routed", result?.Payload);
    }

    [Fact]
    public async Task ProcessAsync_WithMultipleRoutes_SendsToAllStreams()
    {
        var config = new InMemoryProviderConfig();
        config.AddStreams(s =>
        {
            s.AddStream("queue-a");
            s.AddStream("queue-b");
        });
        config.AddRoutes(r => r.Send<string>().ToStream("queue-a", "queue-b"));
        var provider = new InMemoryStreamProvider();

        var middleware = new InMemoryQueueMiddleware(config, provider);
        var message = new Message<string> { Payload = "multi" };
        await middleware.ProcessAsync(message);

        var configA = config.Streams.GetConfig("queue-a")!;
        var configB = config.Streams.GetConfig("queue-b")!;
        var streamA = provider.GetStream(configA);
        var streamB = provider.GetStream(configB);

        var resultA = await streamA!.Read<string>();
        var resultB = await streamB!.Read<string>();

        Assert.Equal("multi", resultA?.Payload);
        Assert.Equal("multi", resultB?.Payload);
    }

    [Fact]
    public async Task ProcessAsync_RouteWithNoStreamConfig_ThrowsInvalidOperation()
    {
        var config = new InMemoryProviderConfig();
        // Route to a stream that doesn't exist in streams config
        config.AddRoutes(r => r.Send<string>().ToStream("nonexistent"));
        var provider = new InMemoryStreamProvider();

        var middleware = new InMemoryQueueMiddleware(config, provider);
        var message = new Message<string> { Payload = "test" };

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => middleware.ProcessAsync(message));
    }
}
