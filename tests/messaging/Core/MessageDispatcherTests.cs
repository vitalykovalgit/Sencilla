namespace Sencilla.Messaging.Tests;

public class MessageDispatcherTests
{
    // Shared tracking lists for order verification
    private static readonly List<Message> SharedMessages1 = [];
    private static readonly List<Message> SharedMessages2 = [];
    private static readonly List<int> SharedOrder = [];

    public MessageDispatcherTests()
    {
        SharedMessages1.Clear();
        SharedMessages2.Clear();
        SharedOrder.Clear();
    }

    [Fact]
    public async Task Send_Payload_CreatesMessageAndProcesses()
    {
        var services = new ServiceCollection();
        services.AddSingleton<MiddlewareA>();
        var config = new MessagingConfig();
        config.Middlewares.Add(typeof(MiddlewareA));
        var sp = services.BuildServiceProvider();

        var dispatcher = new MessageDispatcher(sp, config);
        await dispatcher.Send("hello");

        Assert.Single(SharedMessages1);
        var msg = SharedMessages1[0] as Message<string>;
        Assert.NotNull(msg);
        Assert.Equal("hello", msg!.Payload);
    }

    [Fact]
    public async Task Send_Message_ProcessesThroughAllMiddlewares()
    {
        var services = new ServiceCollection();
        services.AddSingleton<MiddlewareA>();
        services.AddSingleton<MiddlewareB>();
        var config = new MessagingConfig();
        config.Middlewares.Add(typeof(MiddlewareA));
        config.Middlewares.Add(typeof(MiddlewareB));
        var sp = services.BuildServiceProvider();

        var dispatcher = new MessageDispatcher(sp, config);
        var message = new Message<int> { Payload = 42 };
        await dispatcher.Send(message);

        Assert.Single(SharedMessages1);
        Assert.Single(SharedMessages2);
    }

    [Fact]
    public async Task Send_NoMiddlewares_DoesNotThrow()
    {
        var services = new ServiceCollection();
        var config = new MessagingConfig();
        var sp = services.BuildServiceProvider();

        var dispatcher = new MessageDispatcher(sp, config);
        await dispatcher.Send("test");
    }

    [Fact]
    public async Task Send_MultipleMessages_AllProcessed()
    {
        var services = new ServiceCollection();
        services.AddSingleton<MiddlewareA>();
        var config = new MessagingConfig();
        config.Middlewares.Add(typeof(MiddlewareA));
        var sp = services.BuildServiceProvider();

        var dispatcher = new MessageDispatcher(sp, config);
        await dispatcher.Send("msg1");
        await dispatcher.Send("msg2");
        await dispatcher.Send("msg3");

        Assert.Equal(3, SharedMessages1.Count);
    }

    [Fact]
    public async Task Send_WithCancellationToken_PassesToMiddleware()
    {
        var services = new ServiceCollection();
        services.AddSingleton<TokenTrackingMiddleware>();
        var config = new MessagingConfig();
        config.Middlewares.Add(typeof(TokenTrackingMiddleware));
        var sp = services.BuildServiceProvider();

        var dispatcher = new MessageDispatcher(sp, config);
        using var cts = new CancellationTokenSource();
        await dispatcher.Send("test", cts.Token);

        var middleware = sp.GetRequiredService<TokenTrackingMiddleware>();
        Assert.Single(middleware.ReceivedTokens);
        Assert.Equal(cts.Token, middleware.ReceivedTokens[0]);
    }

    [Fact]
    public async Task Send_MiddlewaresExecuteInOrder()
    {
        var services = new ServiceCollection();
        services.AddSingleton<OrderMiddleware1>();
        services.AddSingleton<OrderMiddleware2>();
        services.AddSingleton<OrderMiddleware3>();
        var config = new MessagingConfig();
        config.Middlewares.Add(typeof(OrderMiddleware1));
        config.Middlewares.Add(typeof(OrderMiddleware2));
        config.Middlewares.Add(typeof(OrderMiddleware3));
        var sp = services.BuildServiceProvider();

        var dispatcher = new MessageDispatcher(sp, config);
        await dispatcher.Send("test");

        Assert.Equal([1, 2, 3], SharedOrder);
    }

    private class MiddlewareA : IMessageMiddleware
    {
        public Task ProcessAsync<T>(Message<T>? msg, CancellationToken cancellationToken = default)
        {
            if (msg != null) SharedMessages1.Add(msg);
            return Task.CompletedTask;
        }
    }

    private class MiddlewareB : IMessageMiddleware
    {
        public Task ProcessAsync<T>(Message<T>? msg, CancellationToken cancellationToken = default)
        {
            if (msg != null) SharedMessages2.Add(msg);
            return Task.CompletedTask;
        }
    }

    private class TokenTrackingMiddleware : IMessageMiddleware
    {
        public List<CancellationToken> ReceivedTokens { get; } = [];

        public Task ProcessAsync<T>(Message<T>? msg, CancellationToken cancellationToken = default)
        {
            ReceivedTokens.Add(cancellationToken);
            return Task.CompletedTask;
        }
    }

    private class OrderMiddleware1 : IMessageMiddleware
    {
        public Task ProcessAsync<T>(Message<T>? msg, CancellationToken cancellationToken = default)
        {
            SharedOrder.Add(1);
            return Task.CompletedTask;
        }
    }

    private class OrderMiddleware2 : IMessageMiddleware
    {
        public Task ProcessAsync<T>(Message<T>? msg, CancellationToken cancellationToken = default)
        {
            SharedOrder.Add(2);
            return Task.CompletedTask;
        }
    }

    private class OrderMiddleware3 : IMessageMiddleware
    {
        public Task ProcessAsync<T>(Message<T>? msg, CancellationToken cancellationToken = default)
        {
            SharedOrder.Add(3);
            return Task.CompletedTask;
        }
    }
}
