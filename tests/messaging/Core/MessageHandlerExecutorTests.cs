namespace Sencilla.Messaging.Tests;

public class MessageHandlerExecutorTests
{
    [Fact]
    public async Task ExecuteAsync_InvokesMessageHandler()
    {
        var handler = new TestMessageHandler();
        var services = new ServiceCollection();
        services.AddSingleton<IMessageHandler<Message<string>>>(handler);
        var sp = services.BuildServiceProvider();

        var executor = new MessageHandlerExecutor();
        var message = new Message<string> { Payload = "hello" };
        await executor.ExecuteAsync(message, sp);

        Assert.Single(handler.HandledMessages);
        Assert.Equal("hello", handler.HandledMessages[0].Payload);
    }

    [Fact]
    public async Task ExecuteAsync_InvokesPayloadHandler()
    {
        var handler = new TestPayloadHandler();
        var services = new ServiceCollection();
        services.AddSingleton<IMessageHandler<string>>(handler);
        var sp = services.BuildServiceProvider();

        var executor = new MessageHandlerExecutor();
        var message = new Message<string> { Payload = "hello" };
        await executor.ExecuteAsync(message, sp);

        Assert.Single(handler.HandledPayloads);
        Assert.Equal("hello", handler.HandledPayloads[0]);
    }

    [Fact]
    public async Task ExecuteAsync_InvokesBothHandlerTypes()
    {
        var msgHandler = new TestMessageHandler();
        var payloadHandler = new TestPayloadHandler();
        var services = new ServiceCollection();
        services.AddSingleton<IMessageHandler<Message<string>>>(msgHandler);
        services.AddSingleton<IMessageHandler<string>>(payloadHandler);
        var sp = services.BuildServiceProvider();

        var executor = new MessageHandlerExecutor();
        var message = new Message<string> { Payload = "hello" };
        await executor.ExecuteAsync(message, sp);

        Assert.Single(msgHandler.HandledMessages);
        Assert.Single(payloadHandler.HandledPayloads);
    }

    [Fact]
    public async Task ExecuteAsync_SetsProcessedAt()
    {
        var handler = new TestMessageHandler();
        var services = new ServiceCollection();
        services.AddSingleton<IMessageHandler<Message<string>>>(handler);
        var sp = services.BuildServiceProvider();

        var executor = new MessageHandlerExecutor();
        var message = new Message<string> { Payload = "hello" };

        Assert.Null(message.ProcessedAt);
        await executor.ExecuteAsync(message, sp);
        Assert.NotNull(message.ProcessedAt);
    }

    [Fact]
    public async Task ExecuteAsync_NoHandlers_DoesNotThrow()
    {
        var services = new ServiceCollection();
        var sp = services.BuildServiceProvider();

        var executor = new MessageHandlerExecutor();
        var message = new Message<string> { Payload = "hello" };

        await executor.ExecuteAsync(message, sp);

        Assert.Null(message.ProcessedAt);
    }

    [Fact]
    public async Task ExecuteAsync_NullPayload_SkipsPayloadHandlers()
    {
        var payloadHandler = new TestPayloadHandler();
        var services = new ServiceCollection();
        services.AddSingleton<IMessageHandler<string>>(payloadHandler);
        var sp = services.BuildServiceProvider();

        var executor = new MessageHandlerExecutor();
        var message = new Message<string> { Payload = null };

        await executor.ExecuteAsync(message, sp);

        Assert.Empty(payloadHandler.HandledPayloads);
    }

    [Fact]
    public async Task ExecuteAsync_ScopedResolution_Works()
    {
        var services = new ServiceCollection();
        services.AddScoped<IMessageHandler<Message<string>>, TestMessageHandler>();
        var sp = services.BuildServiceProvider();

        using var scope = sp.CreateScope();

        var executor = new MessageHandlerExecutor();
        var message = new Message<string> { Payload = "scoped" };

        await executor.ExecuteAsync(message, scope.ServiceProvider);

        Assert.NotNull(message.ProcessedAt);
    }

    private class TestMessageHandler : IMessageHandler<Message<string>>
    {
        public List<Message<string>> HandledMessages { get; } = [];

        public Task HandleAsync(Message<string> message, CancellationToken token)
        {
            HandledMessages.Add(message);
            return Task.CompletedTask;
        }
    }

    private class TestPayloadHandler : IMessageHandler<string>
    {
        public List<string> HandledPayloads { get; } = [];

        public Task HandleAsync(string message, CancellationToken token)
        {
            HandledPayloads.Add(message);
            return Task.CompletedTask;
        }
    }
}
