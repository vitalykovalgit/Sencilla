namespace Sencilla.Messaging.Mediator.Tests;

public class MediatorMiddlewareTests
{
    private static Func<Message<T>, CancellationToken, Task> NoOpNext<T>() => (_, _) => Task.CompletedTask;

    private static MediatorMiddleware CreateMiddleware(IServiceProvider sp, MediatorConfig? config = null)
    {
        var scopeFactory = sp.GetRequiredService<IServiceScopeFactory>();
        var executor = new MessageHandlerExecutor();
        return new MediatorMiddleware(scopeFactory, executor, config ?? new MediatorConfig());
    }

    [Fact]
    public async Task HandleAsync_InvokesMessageHandler()
    {
        var handler = new TestMessageHandler();
        var services = new ServiceCollection();
        services.AddSingleton<IMessageHandler<Message<string>>>(handler);
        var sp = services.BuildServiceProvider();

        var middleware = CreateMiddleware(sp);
        var message = new Message<string> { Payload = "hello" };
        await middleware.HandleAsync(message, NoOpNext<string>());

        Assert.Single(handler.HandledMessages);
        Assert.Equal("hello", handler.HandledMessages[0].Payload);
    }

    [Fact]
    public async Task HandleAsync_InvokesPayloadHandler()
    {
        var handler = new TestPayloadHandler();
        var services = new ServiceCollection();
        services.AddSingleton<IMessageHandler<string>>(handler);
        var sp = services.BuildServiceProvider();

        var middleware = CreateMiddleware(sp);
        var message = new Message<string> { Payload = "hello" };
        await middleware.HandleAsync(message, NoOpNext<string>());

        Assert.Single(handler.HandledPayloads);
        Assert.Equal("hello", handler.HandledPayloads[0]);
    }

    [Fact]
    public async Task HandleAsync_InvokesBothMessageAndPayloadHandlers()
    {
        var msgHandler = new TestMessageHandler();
        var payloadHandler = new TestPayloadHandler();
        var services = new ServiceCollection();
        services.AddSingleton<IMessageHandler<Message<string>>>(msgHandler);
        services.AddSingleton<IMessageHandler<string>>(payloadHandler);
        var sp = services.BuildServiceProvider();

        var middleware = CreateMiddleware(sp);
        var message = new Message<string> { Payload = "hello" };
        await middleware.HandleAsync(message, NoOpNext<string>());

        Assert.Single(msgHandler.HandledMessages);
        Assert.Single(payloadHandler.HandledPayloads);
    }

    [Fact]
    public async Task HandleAsync_MultipleMessageHandlers_AllInvoked()
    {
        var handler1 = new TestMessageHandler();
        var handler2 = new TestMessageHandler();
        var services = new ServiceCollection();
        services.AddSingleton<IMessageHandler<Message<string>>>(handler1);
        services.AddSingleton<IMessageHandler<Message<string>>>(handler2);
        var sp = services.BuildServiceProvider();

        var middleware = CreateMiddleware(sp);
        var message = new Message<string> { Payload = "test" };
        await middleware.HandleAsync(message, NoOpNext<string>());

        Assert.Single(handler1.HandledMessages);
        Assert.Single(handler2.HandledMessages);
    }

    [Fact]
    public async Task HandleAsync_MultiplePayloadHandlers_AllInvoked()
    {
        var handler1 = new TestPayloadHandler();
        var handler2 = new TestPayloadHandler();
        var services = new ServiceCollection();
        services.AddSingleton<IMessageHandler<string>>(handler1);
        services.AddSingleton<IMessageHandler<string>>(handler2);
        var sp = services.BuildServiceProvider();

        var middleware = CreateMiddleware(sp);
        var message = new Message<string> { Payload = "test" };
        await middleware.HandleAsync(message, NoOpNext<string>());

        Assert.Single(handler1.HandledPayloads);
        Assert.Single(handler2.HandledPayloads);
    }

    [Fact]
    public async Task HandleAsync_NoHandlers_DoesNotThrow()
    {
        var services = new ServiceCollection();
        var sp = services.BuildServiceProvider();

        var middleware = CreateMiddleware(sp);
        var message = new Message<string> { Payload = "hello" };

        await middleware.HandleAsync(message, NoOpNext<string>());
    }

    [Fact]
    public async Task HandleAsync_SetsProcessedAt()
    {
        var services = new ServiceCollection();
        services.AddSingleton<IMessageHandler<Message<string>>>(new TestMessageHandler());
        var sp = services.BuildServiceProvider();

        var middleware = CreateMiddleware(sp);
        var message = new Message<string> { Payload = "hello" };

        Assert.Null(message.ProcessedAt);

        await middleware.HandleAsync(message, NoOpNext<string>());

        Assert.NotNull(message.ProcessedAt);
    }

    [Fact]
    public async Task HandleAsync_ProcessedAt_IsUtcNow()
    {
        var services = new ServiceCollection();
        services.AddSingleton<IMessageHandler<Message<string>>>(new TestMessageHandler());
        var sp = services.BuildServiceProvider();

        var middleware = CreateMiddleware(sp);
        var message = new Message<string> { Payload = "hello" };

        var before = DateTime.UtcNow;
        await middleware.HandleAsync(message, NoOpNext<string>());
        var after = DateTime.UtcNow;

        Assert.InRange(message.ProcessedAt!.Value, before, after);
    }

    [Fact]
    public async Task HandleAsync_NullPayload_SkipsPayloadHandlers()
    {
        var handler = new TestPayloadHandler();
        var services = new ServiceCollection();
        services.AddSingleton<IMessageHandler<string>>(handler);
        var sp = services.BuildServiceProvider();

        var middleware = CreateMiddleware(sp);
        var message = new Message<string> { Payload = null };

        await middleware.HandleAsync(message, NoOpNext<string>());

        Assert.Empty(handler.HandledPayloads);
    }

    [Fact]
    public async Task HandleAsync_PassesCancellationToken_ToMessageHandler()
    {
        var handler = new TokenTrackingMessageHandler();
        var services = new ServiceCollection();
        services.AddSingleton<IMessageHandler<Message<string>>>(handler);
        var sp = services.BuildServiceProvider();

        var middleware = CreateMiddleware(sp);
        using var cts = new CancellationTokenSource();
        var message = new Message<string> { Payload = "hello" };
        await middleware.HandleAsync(message, NoOpNext<string>(), cts.Token);

        Assert.Single(handler.ReceivedTokens);
        Assert.Equal(cts.Token, handler.ReceivedTokens[0]);
    }

    [Fact]
    public async Task HandleAsync_PassesCancellationToken_ToPayloadHandler()
    {
        var handler = new TokenTrackingPayloadHandler();
        var services = new ServiceCollection();
        services.AddSingleton<IMessageHandler<string>>(handler);
        var sp = services.BuildServiceProvider();

        var middleware = CreateMiddleware(sp);
        using var cts = new CancellationTokenSource();
        var message = new Message<string> { Payload = "hello" };
        await middleware.HandleAsync(message, NoOpNext<string>(), cts.Token);

        Assert.Single(handler.ReceivedTokens);
        Assert.Equal(cts.Token, handler.ReceivedTokens[0]);
    }

    [Fact]
    public async Task HandleAsync_UsesScopedServices()
    {
        var services = new ServiceCollection();
        services.AddScoped<IMessageHandler<Message<string>>, ScopedMessageHandler>();
        var sp = services.BuildServiceProvider();

        var middleware = CreateMiddleware(sp);
        var msg1 = new Message<string> { Payload = "first" };
        var msg2 = new Message<string> { Payload = "second" };

        await middleware.HandleAsync(msg1, NoOpNext<string>());
        await middleware.HandleAsync(msg2, NoOpNext<string>());

        Assert.NotNull(msg1.ProcessedAt);
        Assert.NotNull(msg2.ProcessedAt);
    }

    [Fact]
    public async Task HandleAsync_HandlerThrows_PropagatesException()
    {
        var services = new ServiceCollection();
        services.AddSingleton<IMessageHandler<Message<string>>>(new ThrowingMessageHandler());
        var sp = services.BuildServiceProvider();

        var middleware = CreateMiddleware(sp);
        var message = new Message<string> { Payload = "hello" };

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => middleware.HandleAsync(message, NoOpNext<string>()));
    }

    [Fact]
    public async Task HandleAsync_PayloadHandlerThrows_PropagatesException()
    {
        var services = new ServiceCollection();
        services.AddSingleton<IMessageHandler<string>>(new ThrowingPayloadHandler());
        var sp = services.BuildServiceProvider();

        var middleware = CreateMiddleware(sp);
        var message = new Message<string> { Payload = "hello" };

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => middleware.HandleAsync(message, NoOpNext<string>()));
    }

    [Fact]
    public async Task HandleAsync_ComplexPayload_HandledCorrectly()
    {
        var handler = new ComplexPayloadHandler();
        var services = new ServiceCollection();
        services.AddSingleton<IMessageHandler<OrderCommand>>(handler);
        var sp = services.BuildServiceProvider();

        var middleware = CreateMiddleware(sp);
        var payload = new OrderCommand { OrderId = 42, ProductName = "Widget" };
        var message = new Message<OrderCommand> { Payload = payload };
        await middleware.HandleAsync(message, NoOpNext<OrderCommand>());

        Assert.Single(handler.HandledPayloads);
        Assert.Equal(42, handler.HandledPayloads[0].OrderId);
        Assert.Equal("Widget", handler.HandledPayloads[0].ProductName);
    }

    [Fact]
    public async Task HandleAsync_ValueTypePayload_HandledCorrectly()
    {
        var handler = new IntPayloadHandler();
        var services = new ServiceCollection();
        services.AddSingleton<IMessageHandler<int>>(handler);
        var sp = services.BuildServiceProvider();

        var middleware = CreateMiddleware(sp);
        var message = new Message<int> { Payload = 42 };
        await middleware.HandleAsync(message, NoOpNext<int>());

        Assert.Single(handler.HandledValues);
        Assert.Equal(42, handler.HandledValues[0]);
    }

    [Fact]
    public async Task HandleAsync_CallsNext()
    {
        var services = new ServiceCollection();
        var sp = services.BuildServiceProvider();
        var middleware = CreateMiddleware(sp);
        var nextCalled = false;

        var message = new Message<string> { Payload = "hello" };
        await middleware.HandleAsync(message, (_, _) =>
        {
            nextCalled = true;
            return Task.CompletedTask;
        });

        Assert.True(nextCalled);
    }

    [Fact]
    public async Task HandleAsync_DisableAll_SkipsHandlersButCallsNext()
    {
        var handler = new TestMessageHandler();
        var services = new ServiceCollection();
        services.AddSingleton<IMessageHandler<Message<string>>>(handler);
        var sp = services.BuildServiceProvider();

        var config = new MediatorConfig();
        config.DisableAll();

        var middleware = CreateMiddleware(sp, config);
        var nextCalled = false;
        var message = new Message<string> { Payload = "hello" };

        await middleware.HandleAsync(message, (_, _) =>
        {
            nextCalled = true;
            return Task.CompletedTask;
        });

        Assert.Empty(handler.HandledMessages);
        Assert.True(nextCalled);
    }

    [Fact]
    public async Task HandleAsync_AllowAll_HandlersAreCalled()
    {
        var handler = new TestMessageHandler();
        var services = new ServiceCollection();
        services.AddSingleton<IMessageHandler<Message<string>>>(handler);
        var sp = services.BuildServiceProvider();

        var config = new MediatorConfig();
        config.AllowAll();

        var middleware = CreateMiddleware(sp, config);
        var message = new Message<string> { Payload = "hello" };
        await middleware.HandleAsync(message, NoOpNext<string>());

        Assert.Single(handler.HandledMessages);
    }

    [Fact]
    public async Task HandleAsync_DisableSpecificType_OnlyThatTypeSkipped()
    {
        var stringHandler = new TestMessageHandler();
        var intHandler = new IntPayloadHandler();
        var services = new ServiceCollection();
        services.AddSingleton<IMessageHandler<Message<string>>>(stringHandler);
        services.AddSingleton<IMessageHandler<int>>(intHandler);
        var sp = services.BuildServiceProvider();

        var config = new MediatorConfig();
        config.Disable<string>();

        var middleware = CreateMiddleware(sp, config);

        var stringMsg = new Message<string> { Payload = "hello" };
        await middleware.HandleAsync(stringMsg, NoOpNext<string>());

        var intMsg = new Message<int> { Payload = 42 };
        await middleware.HandleAsync(intMsg, NoOpNext<int>());

        Assert.Empty(stringHandler.HandledMessages);
        Assert.Single(intHandler.HandledValues);
    }

    [Fact]
    public async Task HandleAsync_AllowAfterDisableAll_OnlyAllowedTypeHandled()
    {
        var handler = new TestMessageHandler();
        var intHandler = new IntPayloadHandler();
        var services = new ServiceCollection();
        services.AddSingleton<IMessageHandler<Message<string>>>(handler);
        services.AddSingleton<IMessageHandler<int>>(intHandler);
        var sp = services.BuildServiceProvider();

        var config = new MediatorConfig();
        config.DisableAll();
        config.Allow<string>();

        var middleware = CreateMiddleware(sp, config);

        var stringMsg = new Message<string> { Payload = "hello" };
        await middleware.HandleAsync(stringMsg, NoOpNext<string>());

        var intMsg = new Message<int> { Payload = 42 };
        await middleware.HandleAsync(intMsg, NoOpNext<int>());

        Assert.Single(handler.HandledMessages);
        Assert.Empty(intHandler.HandledValues);
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
