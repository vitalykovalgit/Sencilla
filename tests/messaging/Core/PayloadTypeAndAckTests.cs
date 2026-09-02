using System.Text.Json;
using Microsoft.Extensions.Logging.Abstractions;

namespace Sencilla.Messaging.Tests;

/// <summary>
/// The wire contract that makes cross-process dispatch work: the dispatcher stamps a resolution
/// key, the consumer resolves it back to a type, and a durable stream learns every outcome.
/// </summary>
public class PayloadTypeAndAckTests
{
    // ── Stamping ──────────────────────────────────────────────────────────────

    [Fact]
    public async Task Send_StampsPayloadTypeNamespaceAndName()
    {
        var dispatcher = new MessageDispatcher(new ServiceCollection().BuildServiceProvider(), new MessagingConfig());
        var message = new Message<PlainPayload> { Payload = new PlainPayload() };

        await dispatcher.Send(message);

        Assert.Equal(typeof(PlainPayload).FullName, message.PayloadType);
        Assert.Equal(typeof(PlainPayload).FullName, message.Namespace);
        Assert.Equal(nameof(PlainPayload), message.Name);
    }

    [Fact]
    public async Task Send_PayloadTypeAttribute_StampsAliasButKeepsRealNamespace()
    {
        var dispatcher = new MessageDispatcher(new ServiceCollection().BuildServiceProvider(), new MessagingConfig());
        var message = new Message<AliasedPayload> { Payload = new AliasedPayload() };

        await dispatcher.Send(message);

        Assert.Equal("tests.aliased", message.PayloadType);
        Assert.Equal(typeof(AliasedPayload).FullName, message.Namespace);
    }

    [Fact]
    public async Task Send_ExplicitPayloadType_IsNotOverwritten()
    {
        var dispatcher = new MessageDispatcher(new ServiceCollection().BuildServiceProvider(), new MessagingConfig());
        var message = new Message<PlainPayload> { Payload = new PlainPayload(), PayloadType = "pinned" };

        await dispatcher.Send(message);

        Assert.Equal("pinned", message.PayloadType);
    }

    [Fact]
    public void Registry_ResolvesAliasAndFullName_AndCachesMisses()
    {
        Assert.Equal(typeof(AliasedPayload), PayloadTypeRegistry.Resolve("tests.aliased"));
        Assert.Equal(typeof(PlainPayload), PayloadTypeRegistry.Resolve(typeof(PlainPayload).FullName!));
        Assert.Null(PayloadTypeRegistry.Resolve("nothing.declares.this"));
        Assert.Null(PayloadTypeRegistry.Resolve("nothing.declares.this"));
    }

    // ── Acknowledgement ───────────────────────────────────────────────────────

    [Fact]
    public async Task Consumer_HandlerSucceeds_Acks()
    {
        var stream = await RunAsync<PlainPayload>(new PlainPayload(), services =>
            services.AddSingleton<IMessageHandler<Message<PlainPayload>>>(new OkHandler()));

        Assert.Single(stream.Acked);
        Assert.Empty(stream.Nacked);
    }

    [Fact]
    public async Task Consumer_HandlerThrows_NacksRetryable()
    {
        var stream = await RunAsync<PlainPayload>(new PlainPayload(), services =>
            services.AddSingleton<IMessageHandler<Message<PlainPayload>>>(new BoomHandler()));

        Assert.Empty(stream.Acked);
        var (_, error, retryable) = Assert.Single(stream.Nacked);
        Assert.True(retryable);
        Assert.StartsWith("message.handler.failed", error);
    }

    [Fact]
    public async Task Consumer_NoRegisteredHandler_NacksNonRetryable()
    {
        var stream = await RunAsync<PlainPayload>(new PlainPayload(), _ => { });

        Assert.Empty(stream.Acked);
        var (_, error, retryable) = Assert.Single(stream.Nacked);
        Assert.False(retryable);
        Assert.StartsWith("message.handler.missing", error);
    }

    [Fact]
    public async Task Consumer_UnresolvablePayloadType_NacksNonRetryable()
    {
        // A message written by a producer whose payload assembly this worker doesn't load:
        // it must go terminal, never be silently skipped.
        var json = JsonSerializer.Serialize(new Message<PlainPayload>
        {
            Payload = new PlainPayload(),
            PayloadType = "some.type.this.process.never.heard.of",
        });

        var stream = await RunJsonAsync(json, _ => { });

        Assert.Empty(stream.Acked);
        var (_, error, retryable) = Assert.Single(stream.Nacked);
        Assert.False(retryable);
        Assert.StartsWith("message.type.unresolved", error);
    }

    [Fact]
    public async Task Consumer_TerminalFailure_RaisesMessageFailed()
    {
        var failures = new FailureHandler();
        var stream = await RunAsync<PlainPayload>(new PlainPayload(), services =>
        {
            services.AddSingleton<IMessageHandler<Message<PlainPayload>>>(new BoomHandler());
            services.AddSingleton<IMessageHandler<MessageFailed<PlainPayload>>>(failures);
        });

        Assert.Single(stream.Nacked);
        var failed = Assert.Single(failures.Seen);
        Assert.StartsWith("message.handler.failed", failed.Error);
        Assert.NotNull(failed.Message.Payload);
    }

    // ── Harness ───────────────────────────────────────────────────────────────

    private static async Task<AckingStream> RunAsync<T>(T payload, Action<IServiceCollection> register)
    {
        var message = new Message<T> { Payload = payload, PayloadType = PayloadTypeRegistry.KeyOf<T>() };
        return await RunJsonAsync(JsonSerializer.Serialize(message), register);
    }

    private static async Task<AckingStream> RunJsonAsync(string json, Action<IServiceCollection> register)
    {
        var services = new ServiceCollection();
        register(services);
        var scopeFactory = services.BuildServiceProvider().GetRequiredService<IServiceScopeFactory>();

        var stream = new AckingStream(json);
        var providerConfig = new ProviderConfig();
        var consumer = new MessageStreamConsumer(
            NullLogger<MessageStreamConsumer>.Instance,
            scopeFactory,
            new MessageHandlerExecutor(),
            new SingleStreamProvider(stream),
            new ConsumerConfig { StreamName = "test-stream" },
            new StreamConfig(providerConfig) { Name = "test-stream" });

        using var cts = new CancellationTokenSource();
        var run = consumer.Execute(cts.Token);
        await Task.Delay(200);
        cts.Cancel();
        await run;

        return stream;
    }

    public class PlainPayload
    {
        public string Value { get; set; } = "v";
    }

    [PayloadType("tests.aliased")]
    public class AliasedPayload
    {
        public string Value { get; set; } = "v";
    }

    private class FailureHandler : IMessageHandler<MessageFailed<PlainPayload>>
    {
        public List<MessageFailed<PlainPayload>> Seen { get; } = [];

        public Task HandleAsync(MessageFailed<PlainPayload> failed, CancellationToken token)
        {
            Seen.Add(failed);
            return Task.CompletedTask;
        }
    }

    private class OkHandler : IMessageHandler<Message<PlainPayload>>
    {
        public Task HandleAsync(Message<PlainPayload> message, CancellationToken token) => Task.CompletedTask;
    }

    private class BoomHandler : IMessageHandler<Message<PlainPayload>>
    {
        public Task HandleAsync(Message<PlainPayload> message, CancellationToken token)
            => throw new InvalidOperationException("boom");
    }

    /// <summary>Yields one message, then blocks; records what the consumer acknowledged.</summary>
    private class AckingStream(string message) : IMessageStream, IMessageStreamAck
    {
        public string Name => "test-stream";
        public List<Guid> Acked { get; } = [];
        public List<(Guid Id, string Error, bool Retryable)> Nacked { get; } = [];

        private bool Sent;

        public Task<string?> Read(CancellationToken cancellationToken = default)
        {
            if (!Sent)
            {
                Sent = true;
                return Task.FromResult<string?>(message);
            }

            var tcs = new TaskCompletionSource<string?>();
            cancellationToken.Register(() => tcs.TrySetCanceled(cancellationToken));
            return tcs.Task;
        }

        public Task<Message<T>?> Read<T>(CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task Write<T>(Message<T>? msg, CancellationToken cancellationToken = default) => Task.CompletedTask;

        public Task Ack(Guid messageId, CancellationToken cancellationToken = default)
        {
            Acked.Add(messageId);
            return Task.CompletedTask;
        }

        /// <summary>Terminal on the first nack — this fake has no attempt budget.</summary>
        public Task<bool> Nack(Guid messageId, string error, bool retryable, CancellationToken cancellationToken = default)
        {
            Nacked.Add((messageId, error, retryable));
            return Task.FromResult(true);
        }
    }

    private class SingleStreamProvider(IMessageStream stream) : IMessageStreamProvider
    {
        public IMessageStream? GetStream(StreamConfig config) => stream;
        public IMessageStream GetOrCreateStream(StreamConfig config) => stream;
    }
}
