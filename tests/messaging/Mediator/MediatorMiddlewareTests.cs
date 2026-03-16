namespace Sencilla.Messaging.Mediator.Tests;

public class MediatorMiddlewareTests
{
    [Fact]
    public async Task ProcessAsync_InvokesMessageHandler()
    {
        var handler = new TestMessageHandler();
        var services = new ServiceCollection();
        services.AddSingleton<IMessageHandler<Message<string>>>(handler);
        var sp = services.BuildServiceProvider();

        var middleware = new MediatorMiddleware(sp);
        var message = new Message<string> { Payload = "hello" };
        await middleware.ProcessAsync(message);

        Assert.Single(handler.HandledMessages);
        Assert.Equal("hello", handler.HandledMessages[0].Payload);
    }

    [Fact]
    public async Task ProcessAsync_InvokesPayloadHandler()
    {
        var handler = new TestPayloadHandler();
        var services = new ServiceCollection();
        services.AddSingleton<IMessageHandler<string>>(handler);
        var sp = services.BuildServiceProvider();

        var middleware = new MediatorMiddleware(sp);
        var message = new Message<string> { Payload = "hello" };
        await middleware.ProcessAsync(message);

        Assert.Single(handler.HandledPayloads);
        Assert.Equal("hello", handler.HandledPayloads[0]);
    }

    [Fact]
    public async Task ProcessAsync_InvokesBothMessageAndPayloadHandlers()
    {
        var msgHandler = new TestMessageHandler();
        var payloadHandler = new TestPayloadHandler();
        var services = new ServiceCollection();
        services.AddSingleton<IMessageHandler<Message<string>>>(msgHandler);
        services.AddSingleton<IMessageHandler<string>>(payloadHandler);
        var sp = services.BuildServiceProvider();

        var middleware = new MediatorMiddleware(sp);
        var message = new Message<string> { Payload = "hello" };
        await middleware.ProcessAsync(message);

        Assert.Single(msgHandler.HandledMessages);
        Assert.Single(payloadHandler.HandledPayloads);
    }

    [Fact]
    public async Task ProcessAsync_MultipleMessageHandlers_AllInvoked()
    {
        var handler1 = new TestMessageHandler();
        var handler2 = new TestMessageHandler();
        var services = new ServiceCollection();
        services.AddSingleton<IMessageHandler<Message<string>>>(handler1);
        services.AddSingleton<IMessageHandler<Message<string>>>(handler2);
        var sp = services.BuildServiceProvider();

        var middleware = new MediatorMiddleware(sp);
        var message = new Message<string> { Payload = "test" };
        await middleware.ProcessAsync(message);

        Assert.Single(handler1.HandledMessages);
        Assert.Single(handler2.HandledMessages);
    }

    [Fact]
    public async Task ProcessAsync_MultiplePayloadHandlers_AllInvoked()
    {
        var handler1 = new TestPayloadHandler();
        var handler2 = new TestPayloadHandler();
        var services = new ServiceCollection();
        services.AddSingleton<IMessageHandler<string>>(handler1);
        services.AddSingleton<IMessageHandler<string>>(handler2);
        var sp = services.BuildServiceProvider();

        var middleware = new MediatorMiddleware(sp);
        var message = new Message<string> { Payload = "test" };
        await middleware.ProcessAsync(message);

        Assert.Single(handler1.HandledPayloads);
        Assert.Single(handler2.HandledPayloads);
    }

    [Fact]
    public async Task ProcessAsync_NoHandlers_DoesNotThrow()
    {
        var services = new ServiceCollection();
        var sp = services.BuildServiceProvider();

        var middleware = new MediatorMiddleware(sp);
        var message = new Message<string> { Payload = "hello" };

        await middleware.ProcessAsync(message);
    }

    [Fact]
    public async Task ProcessAsync_SetsProcessedAt()
    {
        var services = new ServiceCollection();
        services.AddSingleton<IMessageHandler<Message<string>>>(new TestMessageHandler());
        var sp = services.BuildServiceProvider();

        var middleware = new MediatorMiddleware(sp);
        var message = new Message<string> { Payload = "hello" };

        Assert.Null(message.ProcessedAt);

        await middleware.ProcessAsync(message);

        Assert.NotNull(message.ProcessedAt);
    }

    [Fact]
    public async Task ProcessAsync_ProcessedAt_IsUtcNow()
    {
        var services = new ServiceCollection();
        services.AddSingleton<IMessageHandler<Message<string>>>(new TestMessageHandler());
        var sp = services.BuildServiceProvider();

        var middleware = new MediatorMiddleware(sp);
        var message = new Message<string> { Payload = "hello" };

        var before = DateTime.UtcNow;
        await middleware.ProcessAsync(message);
        var after = DateTime.UtcNow;

        Assert.InRange(message.ProcessedAt!.Value, before, after);
    }

    [Fact]
    public async Task ProcessAsync_NullMessage_DoesNotThrow()
    {
        var handler = new TestMessageHandler();
        var services = new ServiceCollection();
        services.AddSingleton<IMessageHandler<Message<string>>>(handler);
        var sp = services.BuildServiceProvider();

        var middleware = new MediatorMiddleware(sp);
        await middleware.ProcessAsync<string>(null);

        Assert.Empty(handler.HandledMessages);
    }

    [Fact]
    public async Task ProcessAsync_NullPayload_SkipsPayloadHandlers()
    {
        var handler = new TestPayloadHandler();
        var services = new ServiceCollection();
        services.AddSingleton<IMessageHandler<string>>(handler);
        var sp = services.BuildServiceProvider();

        var middleware = new MediatorMiddleware(sp);
        var message = new Message<string> { Payload = null };

        await middleware.ProcessAsync(message);

        Assert.Empty(handler.HandledPayloads);
    }

    [Fact]
    public async Task ProcessAsync_PassesCancellationToken_ToMessageHandler()
    {
        var handler = new TokenTrackingMessageHandler();
        var services = new ServiceCollection();
        services.AddSingleton<IMessageHandler<Message<string>>>(handler);
        var sp = services.BuildServiceProvider();

        var middleware = new MediatorMiddleware(sp);
        using var cts = new CancellationTokenSource();
        var message = new Message<string> { Payload = "hello" };
        await middleware.ProcessAsync(message, cts.Token);

        Assert.Single(handler.ReceivedTokens);
        Assert.Equal(cts.Token, handler.ReceivedTokens[0]);
    }

    [Fact]
    public async Task ProcessAsync_PassesCancellationToken_ToPayloadHandler()
    {
        var handler = new TokenTrackingPayloadHandler();
        var services = new ServiceCollection();
        services.AddSingleton<IMessageHandler<string>>(handler);
        var sp = services.BuildServiceProvider();

        var middleware = new MediatorMiddleware(sp);
        using var cts = new CancellationTokenSource();
        var message = new Message<string> { Payload = "hello" };
        await middleware.ProcessAsync(message, cts.Token);

        Assert.Single(handler.ReceivedTokens);
        Assert.Equal(cts.Token, handler.ReceivedTokens[0]);
    }

    [Fact]
    public async Task ProcessAsync_UsesScopedServices()
    {
        var services = new ServiceCollection();
        services.AddScoped<IMessageHandler<Message<string>>, ScopedMessageHandler>();
        var sp = services.BuildServiceProvider();

        var middleware = new MediatorMiddleware(sp);
        var msg1 = new Message<string> { Payload = "first" };
        var msg2 = new Message<string> { Payload = "second" };

        await middleware.ProcessAsync(msg1);
        await middleware.ProcessAsync(msg2);

        // Each call creates a new scope so ScopedMessageHandler is a new instance each time
        Assert.NotNull(msg1.ProcessedAt);
        Assert.NotNull(msg2.ProcessedAt);
    }

    [Fact]
    public async Task ProcessAsync_HandlerThrows_PropagatesException()
    {
        var services = new ServiceCollection();
        services.AddSingleton<IMessageHandler<Message<string>>>(new ThrowingMessageHandler());
        var sp = services.BuildServiceProvider();

        var middleware = new MediatorMiddleware(sp);
        var message = new Message<string> { Payload = "hello" };

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => middleware.ProcessAsync(message));
    }

    [Fact]
    public async Task ProcessAsync_PayloadHandlerThrows_PropagatesException()
    {
        var services = new ServiceCollection();
        services.AddSingleton<IMessageHandler<string>>(new ThrowingPayloadHandler());
        var sp = services.BuildServiceProvider();

        var middleware = new MediatorMiddleware(sp);
        var message = new Message<string> { Payload = "hello" };

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => middleware.ProcessAsync(message));
    }

    [Fact]
    public async Task ProcessAsync_ComplexPayload_HandledCorrectly()
    {
        var handler = new ComplexPayloadHandler();
        var services = new ServiceCollection();
        services.AddSingleton<IMessageHandler<OrderCommand>>(handler);
        var sp = services.BuildServiceProvider();

        var middleware = new MediatorMiddleware(sp);
        var payload = new OrderCommand { OrderId = 42, ProductName = "Widget" };
        var message = new Message<OrderCommand> { Payload = payload };
        await middleware.ProcessAsync(message);

        Assert.Single(handler.HandledPayloads);
        Assert.Equal(42, handler.HandledPayloads[0].OrderId);
        Assert.Equal("Widget", handler.HandledPayloads[0].ProductName);
    }

    [Fact]
    public async Task ProcessAsync_ValueTypePayload_HandledCorrectly()
    {
        var handler = new IntPayloadHandler();
        var services = new ServiceCollection();
        services.AddSingleton<IMessageHandler<int>>(handler);
        var sp = services.BuildServiceProvider();

        var middleware = new MediatorMiddleware(sp);
        var message = new Message<int> { Payload = 42 };
        await middleware.ProcessAsync(message);

        Assert.Single(handler.HandledValues);
        Assert.Equal(42, handler.HandledValues[0]);
    }

    // Test helpers

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

    private class TokenTrackingMessageHandler : IMessageHandler<Message<string>>
    {
        public List<CancellationToken> ReceivedTokens { get; } = [];

        public Task HandleAsync(Message<string> message, CancellationToken token)
        {
            ReceivedTokens.Add(token);
            return Task.CompletedTask;
        }
    }

    private class TokenTrackingPayloadHandler : IMessageHandler<string>
    {
        public List<CancellationToken> ReceivedTokens { get; } = [];

        public Task HandleAsync(string message, CancellationToken token)
        {
            ReceivedTokens.Add(token);
            return Task.CompletedTask;
        }
    }

    private class ScopedMessageHandler : IMessageHandler<Message<string>>
    {
        public Task HandleAsync(Message<string> message, CancellationToken token) => Task.CompletedTask;
    }

    private class ThrowingMessageHandler : IMessageHandler<Message<string>>
    {
        public Task HandleAsync(Message<string> message, CancellationToken token)
        {
            throw new InvalidOperationException("Handler failed");
        }
    }

    private class ThrowingPayloadHandler : IMessageHandler<string>
    {
        public Task HandleAsync(string message, CancellationToken token)
        {
            throw new InvalidOperationException("Payload handler failed");
        }
    }

    private class OrderCommand
    {
        public int OrderId { get; set; }
        public string? ProductName { get; set; }
    }

    private class ComplexPayloadHandler : IMessageHandler<OrderCommand>
    {
        public List<OrderCommand> HandledPayloads { get; } = [];

        public Task HandleAsync(OrderCommand message, CancellationToken token)
        {
            HandledPayloads.Add(message);
            return Task.CompletedTask;
        }
    }

    private class IntPayloadHandler : IMessageHandler<int>
    {
        public List<int> HandledValues { get; } = [];

        public Task HandleAsync(int message, CancellationToken token)
        {
            HandledValues.Add(message);
            return Task.CompletedTask;
        }
    }
}
