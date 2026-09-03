using System.Text.Json;
using Microsoft.Extensions.Logging.Abstractions;

namespace Sencilla.Messaging.Tests;

/// <summary>
/// A durable delivery is one unit: the handler's work, the acknowledgement made from inside the
/// handler's scope, and the commit — or none of them.
/// </summary>
public class TransactionalConsumeTests
{
    public class Payload { public string? Value { get; set; } }

    /// <summary>Scoped marker so a test can prove two parties saw the same scope.</summary>
    private class ScopeMarker;

    private class Handler(ScopeMarker marker) : IMessageHandler<Message<Payload>>
    {
        public static ScopeMarker? SeenScope;
        public static bool Throw;
        public Task HandleAsync(Message<Payload> message, CancellationToken token)
        {
            SeenScope = marker;
            if (Throw) throw new InvalidOperationException("handler failed");
            return Task.CompletedTask;
        }
    }

    private class Transaction : IDbTransaction
    {
        public int Commits, Rollbacks, Disposes;
        public Task CommitAsync(CancellationToken token = default) { Commits++; return Task.CompletedTask; }
        public Task RollbackAsync(CancellationToken token = default) { Rollbacks++; return Task.CompletedTask; }
        public void Dispose() => Disposes++;
        public ValueTask DisposeAsync() { Disposes++; return ValueTask.CompletedTask; }
    }

    private class Transactions : ITransactionFactory
    {
        public List<Transaction> Begun { get; } = [];
        public Task<IDbTransaction> Begin(CancellationToken token = default)
        {
            var tx = new Transaction();
            Begun.Add(tx);
            return Task.FromResult<IDbTransaction>(tx);
        }
    }

    private class AckStream(string json) : IMessageStream, IMessageStreamAck
    {
        private bool Delivered;
        public string Name => "durable";
        public int PlainAcks, Nacks;
        public ScopeMarker? AckScope;

        public Task<string?> Read(CancellationToken cancellationToken = default)
        {
            if (!Delivered) { Delivered = true; return Task.FromResult<string?>(json); }
            var tcs = new TaskCompletionSource<string?>();
            cancellationToken.Register(() => tcs.TrySetCanceled(cancellationToken));
            return tcs.Task;
        }
        public Task<Message<T>?> Read<T>(CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task Write<T>(Message<T>? message, CancellationToken cancellationToken = default) => Task.CompletedTask;

        public Task Ack(Guid messageId, CancellationToken cancellationToken = default) { PlainAcks++; return Task.CompletedTask; }
        public Task Ack(Guid messageId, IServiceProvider scopedProvider, CancellationToken cancellationToken = default)
        {
            AckScope = scopedProvider.GetRequiredService<ScopeMarker>();
            return Task.CompletedTask;
        }
        public Task<bool> Nack(Guid messageId, string error, bool retryable, CancellationToken cancellationToken = default) { Nacks++; return Task.FromResult(false); }
    }

    private class Provider(IMessageStream stream) : IMessageStreamProvider
    {
        public IMessageStream? GetStream(StreamConfig config) => stream;
        public IMessageStream GetOrCreateStream(StreamConfig config) => stream;
    }

    private static async Task<(AckStream Stream, Transactions Transactions)> RunOnce(bool throwInHandler, bool withTransactions = true)
    {
        Handler.Throw = throwInHandler;
        Handler.SeenScope = null;

        var services = new ServiceCollection();
        services.AddScoped<ScopeMarker>();
        services.AddScoped<IMessageHandler<Message<Payload>>, Handler>();
        var transactions = new Transactions();
        if (withTransactions)
            services.AddSingleton<ITransactionFactory>(transactions);
        var sp = services.BuildServiceProvider();

        var json = JsonSerializer.Serialize(new Message<Payload> { Payload = new() { Value = "x" }, PayloadType = PayloadTypeRegistry.KeyOf<Payload>() });
        var stream = new AckStream(json);
        var consumerConfig = new ConsumerConfig { StreamName = "durable" };
        var streamConfig = new StreamConfig(new ProviderConfig()) { Name = "durable" };
        var consumer = new MessageStreamConsumer(NullLogger<MessageStreamConsumer>.Instance,
            sp.GetRequiredService<IServiceScopeFactory>(), new MessageHandlerExecutor(), new Provider(stream), consumerConfig, streamConfig);

        using var cts = new CancellationTokenSource();
        var run = consumer.Execute(cts.Token);
        await Task.Delay(200);
        cts.Cancel();
        await run;

        return (stream, transactions);
    }

    [Fact]
    public async Task Success_BeginsHandlesAcksInsideTheScope_ThenCommits()
    {
        var (stream, transactions) = await RunOnce(throwInHandler: false);

        var tx = Assert.Single(transactions.Begun);
        Assert.NotNull(Handler.SeenScope);
        Assert.Same(Handler.SeenScope, stream.AckScope);   // the ack ran in the handler's scope → same DbContext, same transaction
        Assert.Equal(1, tx.Commits);
        Assert.Equal(0, stream.PlainAcks);                  // the scoped ack replaced the out-of-band one
        Assert.Equal(0, stream.Nacks);
    }

    [Fact]
    public async Task HandlerFailure_NeverCommits_AndNacks()
    {
        var (stream, transactions) = await RunOnce(throwInHandler: true);

        var tx = Assert.Single(transactions.Begun);
        Assert.Equal(0, tx.Commits);
        Assert.True(tx.Disposes > 0);                       // disposed uncommitted → rolled back
        Assert.Null(stream.AckScope);
        Assert.Equal(1, stream.Nacks);
    }

    [Fact]
    public async Task NoTransactionFactory_StillHandlesAndAcks()
    {
        var (stream, transactions) = await RunOnce(throwInHandler: false, withTransactions: false);

        Assert.Empty(transactions.Begun);
        Assert.NotNull(stream.AckScope);
    }
}
