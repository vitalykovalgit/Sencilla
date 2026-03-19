namespace Sencilla.Messaging.Tests;

public class MessageDispatcherEdgeCaseTests
{
    [Fact]
    public async Task Send_NullPayload_StillCreatesMessage()
    {
        var processedMessages = new List<Message>();
        var services = new ServiceCollection();
        services.AddSingleton(new TrackingMiddleware(processedMessages));
        var config = new MessagingConfig();
        config.Middlewares.Add(typeof(TrackingMiddleware));
        var sp = services.BuildServiceProvider();

        var dispatcher = new MessageDispatcher(sp, config);
        await dispatcher.Send<string?>(null);

        Assert.Single(processedMessages);
    }

    [Fact]
    public async Task Send_WithCancelledToken_ThrowsOperationCanceled()
    {
        var services = new ServiceCollection();
        services.AddSingleton<SlowMiddleware>();
        var config = new MessagingConfig();
        config.Middlewares.Add(typeof(SlowMiddleware));
        var sp = services.BuildServiceProvider();

        var dispatcher = new MessageDispatcher(sp, config);
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => dispatcher.Send("test", cts.Token));
    }

    [Fact]
    public async Task Send_MiddlewareThrows_PropagatesException()
    {
        var services = new ServiceCollection();
        services.AddSingleton<ThrowingMiddleware>();
        var config = new MessagingConfig();
        config.Middlewares.Add(typeof(ThrowingMiddleware));
        var sp = services.BuildServiceProvider();

        var dispatcher = new MessageDispatcher(sp, config);

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => dispatcher.Send("test"));
    }

    [Fact]
    public async Task Send_MiddlewareThrows_StopsSubsequentMiddlewares()
    {
        var processedMessages = new List<Message>();
        var services = new ServiceCollection();
        services.AddSingleton<ThrowingMiddleware>();
        services.AddSingleton(new TrackingMiddleware(processedMessages));
        var config = new MessagingConfig();
        config.Middlewares.Add(typeof(ThrowingMiddleware));
        config.Middlewares.Add(typeof(TrackingMiddleware));
        var sp = services.BuildServiceProvider();

        var dispatcher = new MessageDispatcher(sp, config);

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => dispatcher.Send("test"));

        Assert.Empty(processedMessages);
    }

    [Fact]
    public async Task Send_MessageObject_PreservesAllProperties()
    {
        var processedMessages = new List<Message>();
        var services = new ServiceCollection();
        services.AddSingleton(new TrackingMiddleware(processedMessages));
        var config = new MessagingConfig();
        config.Middlewares.Add(typeof(TrackingMiddleware));
        var sp = services.BuildServiceProvider();

        var dispatcher = new MessageDispatcher(sp, config);
        var correlationId = Guid.NewGuid();
        var message = new Message<string>
        {
            Payload = "hello",
            CorrelationId = correlationId,
            Type = MessageType.Event,
            Name = "test-msg"
        };

        await dispatcher.Send(message);

        var received = processedMessages[0] as Message<string>;
        Assert.NotNull(received);
        Assert.Equal("hello", received!.Payload);
        Assert.Equal(correlationId, received.CorrelationId);
        Assert.Equal(MessageType.Event, received.Type);
        Assert.Equal("test-msg", received.Name);
    }

    [Fact]
    public async Task Send_ComplexPayload_PassedThroughMiddleware()
    {
        var processedMessages = new List<Message>();
        var services = new ServiceCollection();
        services.AddSingleton(new TrackingMiddleware(processedMessages));
        var config = new MessagingConfig();
        config.Middlewares.Add(typeof(TrackingMiddleware));
        var sp = services.BuildServiceProvider();

        var dispatcher = new MessageDispatcher(sp, config);
        var payload = new { Name = "test", Items = new[] { 1, 2, 3 } };
        await dispatcher.Send(payload);

        Assert.Single(processedMessages);
    }

    [Fact]
    public async Task Send_DefaultCancellationToken_DoesNotCancel()
    {
        var processedMessages = new List<Message>();
        var services = new ServiceCollection();
        services.AddSingleton(new TrackingMiddleware(processedMessages));
        var config = new MessagingConfig();
        config.Middlewares.Add(typeof(TrackingMiddleware));
        var sp = services.BuildServiceProvider();

        var dispatcher = new MessageDispatcher(sp, config);
        await dispatcher.Send("test");

        Assert.Single(processedMessages);
    }

    [Fact]
    public async Task Send_ExceptionBeforeNext_PropagatesAndStopsChain()
    {
        var processedMessages = new List<Message>();
        var services = new ServiceCollection();
        services.AddSingleton<ThrowBeforeNextMiddleware>();
        services.AddSingleton(new TrackingMiddleware(processedMessages));
        var config = new MessagingConfig();
        config.Middlewares.Add(typeof(ThrowBeforeNextMiddleware));
        config.Middlewares.Add(typeof(TrackingMiddleware));
        var sp = services.BuildServiceProvider();

        var dispatcher = new MessageDispatcher(sp, config);

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => dispatcher.Send("test"));

        Assert.Empty(processedMessages);
    }

    [Fact]
    public async Task Send_ExceptionAfterNext_PropagatesButChainRan()
    {
        var processedMessages = new List<Message>();
        var services = new ServiceCollection();
        services.AddSingleton<ThrowAfterNextMiddleware>();
        services.AddSingleton(new TrackingMiddleware(processedMessages));
        var config = new MessagingConfig();
        config.Middlewares.Add(typeof(ThrowAfterNextMiddleware));
        config.Middlewares.Add(typeof(TrackingMiddleware));
        var sp = services.BuildServiceProvider();

        var dispatcher = new MessageDispatcher(sp, config);

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => dispatcher.Send("test"));

        Assert.Single(processedMessages);
    }

    private class TrackingMiddleware(List<Message> messages) : IMessageMiddleware
    {
        public async Task HandleAsync<T>(Message<T> message, Func<Message<T>, CancellationToken, Task> next, CancellationToken cancellationToken = default)
        {
            messages.Add(message);
            await next(message, cancellationToken);
        }
    }

    private class ThrowingMiddleware : IMessageMiddleware
    {
        public Task HandleAsync<T>(Message<T> message, Func<Message<T>, CancellationToken, Task> next, CancellationToken cancellationToken = default)
        {
            throw new InvalidOperationException("Middleware failed");
        }
    }

    private class SlowMiddleware : IMessageMiddleware
    {
        public async Task HandleAsync<T>(Message<T> message, Func<Message<T>, CancellationToken, Task> next, CancellationToken cancellationToken = default)
        {
            await Task.Delay(5000, cancellationToken);
            await next(message, cancellationToken);
        }
    }

    private class ThrowBeforeNextMiddleware : IMessageMiddleware
    {
        public Task HandleAsync<T>(Message<T> message, Func<Message<T>, CancellationToken, Task> next, CancellationToken cancellationToken = default)
        {
            throw new InvalidOperationException("Before next");
        }
    }

    private class ThrowAfterNextMiddleware : IMessageMiddleware
    {
        public async Task HandleAsync<T>(Message<T> message, Func<Message<T>, CancellationToken, Task> next, CancellationToken cancellationToken = default)
        {
            await next(message, cancellationToken);
            throw new InvalidOperationException("After next");
        }
    }
}
