namespace Sencilla.Messaging.RabbitMQ.Tests;

public class RabbitMQMiddlewareTests
{
    private static Func<Message<T>, CancellationToken, Task> NoOpNext<T>() => (_, _) => Task.CompletedTask;

    [Fact]
    public async Task HandleAsync_NoRoutes_DoesNotThrow()
    {
        var config = new RabbitMQProviderConfig();
        var connectionFactory = new Mock<IRabbitMQConnectionFactory>();
        var provider = new RabbitMQStreamProvider(connectionFactory.Object, config);

        var middleware = new RabbitMQMiddleware(config, provider);
        var message = new Message<string> { Payload = "test" };

        await middleware.HandleAsync(message, NoOpNext<string>());
    }

    [Fact]
    public async Task HandleAsync_CallsNext()
    {
        var config = new RabbitMQProviderConfig();
        var connectionFactory = new Mock<IRabbitMQConnectionFactory>();
        var provider = new RabbitMQStreamProvider(connectionFactory.Object, config);
        var middleware = new RabbitMQMiddleware(config, provider);
        var nextCalled = false;

        var message = new Message<string> { Payload = "test" };
        await middleware.HandleAsync(message, (_, _) =>
        {
            nextCalled = true;
            return Task.CompletedTask;
        });

        Assert.True(nextCalled);
    }

    [Fact]
    public async Task HandleAsync_RouteWithNoStreamConfig_ThrowsInvalidOperation()
    {
        var config = new RabbitMQProviderConfig();
        config.AddRoutes(r => r.Send<string>().ToStream("nonexistent"));
        var connectionFactory = new Mock<IRabbitMQConnectionFactory>();
        var provider = new RabbitMQStreamProvider(connectionFactory.Object, config);

        var middleware = new RabbitMQMiddleware(config, provider);
        var message = new Message<string> { Payload = "test" };

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => middleware.HandleAsync(message, NoOpNext<string>()));
    }

    [Fact]
    public async Task HandleAsync_WithRoute_CreatesStream()
    {
        var config = new RabbitMQProviderConfig();
        config.AddStreams(s => s.AddStream("my-queue"));
        config.AddRoutes(r => r.Send<string>().ToStream("my-queue"));

        var connectionFactory = new Mock<IRabbitMQConnectionFactory>();
        var provider = new RabbitMQStreamProvider(connectionFactory.Object, config);

        var middleware = new RabbitMQMiddleware(config, provider);
        var message = new Message<string> { Payload = "routed" };

        // SendToStream calls GetOrCreateStream which creates the RabbitMQStream
        // The actual publish will fail without a real connection, but the stream is created
        var streamConfig = config.Streams.GetConfig("my-queue")!;

        // Before middleware, no stream exists
        Assert.Null(provider.GetStream(streamConfig));

        // HandleAsync will attempt to send, which creates the stream and tries to publish
        // Since we don't have a real RabbitMQ connection, it will throw
        // But we can verify the stream was created by checking GetStream after catching the error
        try
        {
            await middleware.HandleAsync(message, NoOpNext<string>());
        }
        catch
        {
            // Expected — no real RabbitMQ connection
        }

        // Stream should have been created by GetOrCreateStream
        var stream = provider.GetStream(streamConfig);
        Assert.NotNull(stream);
        Assert.Equal("my-queue", stream!.Name);
    }
}
