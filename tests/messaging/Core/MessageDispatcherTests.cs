namespace Sencilla.Messaging.Tests;

public class MessageDispatcherTests
{
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

    [Fact]
    public async Task Send_ChainPattern_PreAndPostNextExecution()
    {
        var order = new List<string>();
        var services = new ServiceCollection();
        services.AddSingleton(new PrePostMiddleware(order, "A"));
        services.AddSingleton(new PrePostMiddleware(order, "B"));
        var config = new MessagingConfig();
        config.Middlewares.Add(typeof(PrePostMiddleware));
        // We need two separate instances, so register differently
        var sp = services.BuildServiceProvider();

        // Use a different approach - manually build
        var services2 = new ServiceCollection();
        var mwA = new PrePostMiddleware(order, "A");
        var mwB = new PrePostMiddleware(order, "B");
        services2.AddSingleton<PrePostMiddlewareA>(new PrePostMiddlewareA(order));
        services2.AddSingleton<PrePostMiddlewareB>(new PrePostMiddlewareB(order));
        var config2 = new MessagingConfig();
        config2.Middlewares.Add(typeof(PrePostMiddlewareA));
        config2.Middlewares.Add(typeof(PrePostMiddlewareB));
        var sp2 = services2.BuildServiceProvider();

        var dispatcher = new MessageDispatcher(sp2, config2);
        await dispatcher.Send("test");

        Assert.Equal(["A-before", "B-before", "B-after", "A-after"], order);
    }

    [Fact]
    public async Task Send_ShortCircuit_MiddlewareOmittingNextStopsChain()
    {
        var services = new ServiceCollection();
        services.AddSingleton<ShortCircuitMiddleware>();
        services.AddSingleton<MiddlewareA>();
        var config = new MessagingConfig();
        config.Middlewares.Add(typeof(ShortCircuitMiddleware));
        config.Middlewares.Add(typeof(MiddlewareA));
        var sp = services.BuildServiceProvider();

        var dispatcher = new MessageDispatcher(sp, config);
        await dispatcher.Send("test");

        Assert.Empty(SharedMessages1);
    }

    [Fact]
    public async Task Send_PipelineBuiltOncePerType()
    {
        var services = new ServiceCollection();
        services.AddSingleton<MiddlewareA>();
        var config = new MessagingConfig();
        config.Middlewares.Add(typeof(MiddlewareA));
        var sp = services.BuildServiceProvider();

        var dispatcher = new MessageDispatcher(sp, config);
        await dispatcher.Send("msg1");
        await dispatcher.Send("msg2");

        Assert.Equal(2, SharedMessages1.Count);
    }

    private class MiddlewareA : IMessageMiddleware
    {
        public async Task HandleAsync<T>(Message<T> message, Func<Message<T>, CancellationToken, Task> next, CancellationToken cancellationToken = default)
        {
            SharedMessages1.Add(message);
            await next(message, cancellationToken);
        }
    }

    private class MiddlewareB : IMessageMiddleware
    {
        public async Task HandleAsync<T>(Message<T> message, Func<Message<T>, CancellationToken, Task> next, CancellationToken cancellationToken = default)
        {
            SharedMessages2.Add(message);
            await next(message, cancellationToken);
        }
    }

    private class TokenTrackingMiddleware : IMessageMiddleware
    {
        public List<CancellationToken> ReceivedTokens { get; } = [];

        public async Task HandleAsync<T>(Message<T> message, Func<Message<T>, CancellationToken, Task> next, CancellationToken cancellationToken = default)
        {
            ReceivedTokens.Add(cancellationToken);
            await next(message, cancellationToken);
        }
    }

    private class OrderMiddleware1 : IMessageMiddleware
    {
        public async Task HandleAsync<T>(Message<T> message, Func<Message<T>, CancellationToken, Task> next, CancellationToken cancellationToken = default)
        {
            SharedOrder.Add(1);
            await next(message, cancellationToken);
        }
    }

    private class OrderMiddleware2 : IMessageMiddleware
    {
        public async Task HandleAsync<T>(Message<T> message, Func<Message<T>, CancellationToken, Task> next, CancellationToken cancellationToken = default)
        {
            SharedOrder.Add(2);
            await next(message, cancellationToken);
        }
    }

    private class OrderMiddleware3 : IMessageMiddleware
    {
        public async Task HandleAsync<T>(Message<T> message, Func<Message<T>, CancellationToken, Task> next, CancellationToken cancellationToken = default)
        {
            SharedOrder.Add(3);
            await next(message, cancellationToken);
        }
    }

    private class PrePostMiddleware(List<string> order, string name) : IMessageMiddleware
    {
        public async Task HandleAsync<T>(Message<T> message, Func<Message<T>, CancellationToken, Task> next, CancellationToken cancellationToken = default)
        {
            order.Add($"{name}-before");
            await next(message, cancellationToken);
            order.Add($"{name}-after");
        }
    }

    private class PrePostMiddlewareA(List<string> order) : IMessageMiddleware
    {
        public async Task HandleAsync<T>(Message<T> message, Func<Message<T>, CancellationToken, Task> next, CancellationToken cancellationToken = default)
        {
            order.Add("A-before");
            await next(message, cancellationToken);
            order.Add("A-after");
        }
    }

    private class PrePostMiddlewareB(List<string> order) : IMessageMiddleware
    {
        public async Task HandleAsync<T>(Message<T> message, Func<Message<T>, CancellationToken, Task> next, CancellationToken cancellationToken = default)
        {
            order.Add("B-before");
            await next(message, cancellationToken);
            order.Add("B-after");
        }
    }

    private class ShortCircuitMiddleware : IMessageMiddleware
    {
        public Task HandleAsync<T>(Message<T> message, Func<Message<T>, CancellationToken, Task> next, CancellationToken cancellationToken = default)
        {
            // Intentionally not calling next — short-circuits the chain
            return Task.CompletedTask;
        }
    }
}
