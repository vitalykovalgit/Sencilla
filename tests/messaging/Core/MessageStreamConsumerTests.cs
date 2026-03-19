using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Sencilla.Messaging.Tests;

public class MessageStreamConsumerTests
{
    private readonly ILogger<MessageStreamConsumer> Logger = NullLogger<MessageStreamConsumer>.Instance;

    [Fact]
    public async Task Execute_ValidMessage_InvokesHandler()
    {
        var handler = new TestHandler();
        var services = new ServiceCollection();
        services.AddSingleton<IMessageHandler<Message<TestPayload>>>(handler);
        var sp = services.BuildServiceProvider();
        var scopeFactory = sp.GetRequiredService<IServiceScopeFactory>();

        var payload = new TestPayload { Value = "hello" };
        var message = new Message<TestPayload>
        {
            Payload = payload,
            Namespace = typeof(TestPayload).AssemblyQualifiedName
        };
        var json = JsonSerializer.Serialize(message);

        var stream = new FakeStream(json);
        var provider = new FakeStreamProvider(stream);
        var executor = new MessageHandlerExecutor();

        var consumerConfig = new ConsumerConfig { StreamName = "test-stream" };
        var providerConfig = new ProviderConfig();
        var streamConfig = new StreamConfig(providerConfig) { Name = "test-stream" };

        var consumer = new MessageStreamConsumer(Logger, scopeFactory, executor, provider, consumerConfig, streamConfig);

        using var cts = new CancellationTokenSource();
        var task = consumer.Execute(cts.Token);

        // Wait for processing, then cancel
        await Task.Delay(200);
        cts.Cancel();
        await task;

        Assert.Single(handler.Handled);
        Assert.Equal("hello", handler.Handled[0].Payload!.Value);
    }

    [Fact]
    public async Task Execute_EmptyNamespace_SkipsMessage()
    {
        var handler = new TestHandler();
        var services = new ServiceCollection();
        services.AddSingleton<IMessageHandler<Message<TestPayload>>>(handler);
        var sp = services.BuildServiceProvider();
        var scopeFactory = sp.GetRequiredService<IServiceScopeFactory>();

        var message = new Message<TestPayload>
        {
            Payload = new TestPayload { Value = "skip" },
            Namespace = null
        };
        var json = JsonSerializer.Serialize(message);

        var stream = new FakeStream(json);
        var provider = new FakeStreamProvider(stream);
        var executor = new MessageHandlerExecutor();

        var consumerConfig = new ConsumerConfig { StreamName = "test-stream" };
        var providerConfig = new ProviderConfig();
        var streamConfig = new StreamConfig(providerConfig) { Name = "test-stream" };

        var consumer = new MessageStreamConsumer(Logger, scopeFactory, executor, provider, consumerConfig, streamConfig);

        using var cts = new CancellationTokenSource();
        var task = consumer.Execute(cts.Token);

        await Task.Delay(200);
        cts.Cancel();
        await task;

        Assert.Empty(handler.Handled);
    }

    [Fact]
    public async Task Execute_UnknownType_SkipsMessage()
    {
        var handler = new TestHandler();
        var services = new ServiceCollection();
        services.AddSingleton<IMessageHandler<Message<TestPayload>>>(handler);
        var sp = services.BuildServiceProvider();
        var scopeFactory = sp.GetRequiredService<IServiceScopeFactory>();

        var message = new Message<TestPayload>
        {
            Payload = new TestPayload { Value = "unknown" },
            Namespace = "NonExistent.Type, NonExistent.Assembly"
        };
        var json = JsonSerializer.Serialize(message);

        var stream = new FakeStream(json);
        var provider = new FakeStreamProvider(stream);
        var executor = new MessageHandlerExecutor();

        var consumerConfig = new ConsumerConfig { StreamName = "test-stream" };
        var providerConfig = new ProviderConfig();
        var streamConfig = new StreamConfig(providerConfig) { Name = "test-stream" };

        var consumer = new MessageStreamConsumer(Logger, scopeFactory, executor, provider, consumerConfig, streamConfig);

        using var cts = new CancellationTokenSource();
        var task = consumer.Execute(cts.Token);

        await Task.Delay(200);
        cts.Cancel();
        await task;

        Assert.Empty(handler.Handled);
    }

    [Fact]
    public async Task Execute_HandlerThrows_SemaphoreReleased()
    {
        var services = new ServiceCollection();
        services.AddSingleton<IMessageHandler<Message<TestPayload>>>(new ThrowingHandler());
        var sp = services.BuildServiceProvider();
        var scopeFactory = sp.GetRequiredService<IServiceScopeFactory>();

        var payload = new TestPayload { Value = "boom" };
        // Produce two messages — if semaphore leaks, the second blocks forever
        var message1 = new Message<TestPayload>
        {
            Payload = payload,
            Namespace = typeof(TestPayload).AssemblyQualifiedName
        };
        var message2 = new Message<TestPayload>
        {
            Payload = new TestPayload { Value = "after-boom" },
            Namespace = typeof(TestPayload).AssemblyQualifiedName
        };

        var stream = new FakeStream(JsonSerializer.Serialize(message1), JsonSerializer.Serialize(message2));
        var provider = new FakeStreamProvider(stream);
        var executor = new MessageHandlerExecutor();

        // MaxConcurrentHandlers = 1, so if semaphore leaks, second message never processes
        var consumerConfig = new ConsumerConfig { StreamName = "test-stream", MaxConcurrentHandlers = 1 };
        var providerConfig = new ProviderConfig();
        var streamConfig = new StreamConfig(providerConfig) { Name = "test-stream" };

        var consumer = new MessageStreamConsumer(Logger, scopeFactory, executor, provider, consumerConfig, streamConfig);

        using var cts = new CancellationTokenSource();
        var task = consumer.Execute(cts.Token);

        // Give enough time for both messages to be processed
        await Task.Delay(500);
        cts.Cancel();

        // If semaphore leaked, this would hang forever
        var completed = await Task.WhenAny(task, Task.Delay(3000));
        Assert.Same(task, completed);
    }

    [Fact]
    public async Task Execute_Cancellation_GracefulShutdown()
    {
        var handler = new SlowHandler(TimeSpan.FromMilliseconds(500));
        var services = new ServiceCollection();
        services.AddSingleton<IMessageHandler<Message<TestPayload>>>(handler);
        var sp = services.BuildServiceProvider();
        var scopeFactory = sp.GetRequiredService<IServiceScopeFactory>();

        var message = new Message<TestPayload>
        {
            Payload = new TestPayload { Value = "slow" },
            Namespace = typeof(TestPayload).AssemblyQualifiedName
        };
        var json = JsonSerializer.Serialize(message);

        var stream = new FakeStream(json);
        var provider = new FakeStreamProvider(stream);
        var executor = new MessageHandlerExecutor();

        var consumerConfig = new ConsumerConfig { StreamName = "test-stream" };
        var providerConfig = new ProviderConfig();
        var streamConfig = new StreamConfig(providerConfig) { Name = "test-stream" };

        var consumer = new MessageStreamConsumer(Logger, scopeFactory, executor, provider, consumerConfig, streamConfig);

        using var cts = new CancellationTokenSource();
        var task = consumer.Execute(cts.Token);

        // Let the message start processing, then cancel
        await Task.Delay(100);
        cts.Cancel();

        // Should wait for the in-flight message and complete gracefully
        var completed = await Task.WhenAny(task, Task.Delay(3000));
        Assert.Same(task, completed);

        // Handler should have completed (graceful shutdown waits for in-flight)
        Assert.Equal(1, handler.CompletedCount);
    }

    #region Test helpers

    public class TestPayload
    {
        public string? Value { get; set; }
    }

    private class TestHandler : IMessageHandler<Message<TestPayload>>
    {
        public List<Message<TestPayload>> Handled { get; } = [];

        public Task HandleAsync(Message<TestPayload> message, CancellationToken token)
        {
            Handled.Add(message);
            return Task.CompletedTask;
        }
    }

    private class ThrowingHandler : IMessageHandler<Message<TestPayload>>
    {
        public Task HandleAsync(Message<TestPayload> message, CancellationToken token)
        {
            throw new InvalidOperationException("Handler exploded");
        }
    }

    private class SlowHandler(TimeSpan delay) : IMessageHandler<Message<TestPayload>>
    {
        public int CompletedCount;

        public async Task HandleAsync(Message<TestPayload> message, CancellationToken token)
        {
            await Task.Delay(delay, CancellationToken.None); // Ignore cancellation to simulate work
            Interlocked.Increment(ref CompletedCount);
        }
    }

    /// <summary>
    /// A fake stream that yields pre-configured messages, then blocks until cancelled.
    /// </summary>
    private class FakeStream(params string[] messages) : IMessageStream
    {
        public string Name => "fake-stream";
        private int Index;

        public Task<string?> Read(CancellationToken cancellationToken = default)
        {
            if (Index < messages.Length)
            {
                return Task.FromResult<string?>(messages[Index++]);
            }

            // Block until cancellation (simulates an empty queue)
            var tcs = new TaskCompletionSource<string?>();
            cancellationToken.Register(() => tcs.TrySetCanceled(cancellationToken));
            return tcs.Task;
        }

        public Task<Message<T>?> Read<T>(CancellationToken cancellationToken = default)
            => throw new NotImplementedException();

        public Task Write<T>(Message<T>? message, CancellationToken cancellationToken = default)
            => Task.CompletedTask;
    }

    private class FakeStreamProvider(IMessageStream stream) : IMessageStreamProvider
    {
        public IMessageStream? GetStream(StreamConfig config) => stream;
        public IMessageStream GetOrCreateStream(StreamConfig config) => stream;
    }

    #endregion
}
