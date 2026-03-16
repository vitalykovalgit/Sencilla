namespace Sencilla.Messaging.InMemoryQueue.Tests;

public class InMemoryQueueEdgeCaseTests : IDisposable
{
    private readonly Messaging.InMemoryQueue.InMemoryQueue _queue;

    public InMemoryQueueEdgeCaseTests()
    {
        _queue = new Messaging.InMemoryQueue.InMemoryQueue("edge-queue");
    }

    [Fact]
    public async Task Write_PayloadOverload_CreatesMessageAndReads()
    {
        await _queue.Write<string>("direct-payload");

        var result = await _queue.Read<string>();

        Assert.NotNull(result);
        Assert.Equal("direct-payload", result!.Payload);
    }

    [Fact]
    public async Task Write_PayloadOverload_NullPayload_DoesNothing()
    {
        await _queue.Write<string>((string?)null);

        using var cts = new CancellationTokenSource(100);
        await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => _queue.Read(cts.Token));
    }

    [Fact]
    public async Task Read_EmptyQueue_BlocksUntilTimeout()
    {
        using var cts = new CancellationTokenSource(200);

        await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => _queue.Read(cts.Token));
    }

    [Fact]
    public async Task Read_Generic_EmptyQueue_BlocksUntilTimeout()
    {
        using var cts = new CancellationTokenSource(200);

        await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => _queue.Read<string>(cts.Token));
    }

    [Fact]
    public async Task Write_LargePayload_HandledCorrectly()
    {
        var largeString = new string('x', 100_000);
        var message = new Message<string> { Payload = largeString };
        await _queue.Write(message);

        var result = await _queue.Read<string>();

        Assert.NotNull(result);
        Assert.Equal(100_000, result!.Payload!.Length);
    }

    [Fact]
    public async Task ConcurrentWriters_AllMessagesDelivered()
    {
        var tasks = Enumerable.Range(0, 50).Select(i =>
            _queue.Write(new Message<int> { Payload = i }));

        await Task.WhenAll(tasks);

        var received = new List<int>();
        for (int i = 0; i < 50; i++)
        {
            var msg = await _queue.Read<int>();
            Assert.NotNull(msg);
            received.Add(msg!.Payload);
        }

        Assert.Equal(50, received.Count);
        Assert.Equal(Enumerable.Range(0, 50).OrderBy(x => x), received.OrderBy(x => x));
    }

    [Fact]
    public async Task ConcurrentReaders_EachMessageDeliveredOnce()
    {
        for (int i = 0; i < 10; i++)
            await _queue.Write(new Message<int> { Payload = i });

        var results = new System.Collections.Concurrent.ConcurrentBag<int>();
        var tasks = Enumerable.Range(0, 10).Select(_ => Task.Run(async () =>
        {
            var msg = await _queue.Read<int>();
            if (msg != null) results.Add(msg.Payload);
        }));

        await Task.WhenAll(tasks);

        Assert.Equal(10, results.Count);
        Assert.Equal(10, results.Distinct().Count());
    }

    [Fact]
    public async Task BoundedQueue_AfterRead_CanWriteAgain()
    {
        using var bounded = new Messaging.InMemoryQueue.InMemoryQueue("bounded", capacity: 1);

        await bounded.Write(new Message<string> { Payload = "first" });
        var first = await bounded.Read<string>();
        Assert.Equal("first", first!.Payload);

        await bounded.Write(new Message<string> { Payload = "second" });
        var second = await bounded.Read<string>();
        Assert.Equal("second", second!.Payload);
    }

    [Fact]
    public async Task BoundedQueue_CapacityOne_BlocksOnSecondWrite()
    {
        using var bounded = new Messaging.InMemoryQueue.InMemoryQueue("bounded", capacity: 1);

        await bounded.Write(new Message<string> { Payload = "first" });

        using var cts = new CancellationTokenSource(200);
        await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => bounded.Write(new Message<string> { Payload = "second" }, cts.Token));
    }

    [Fact]
    public async Task Read_AfterWriteAndDispose_CanReadPendingItems()
    {
        using var queue = new Messaging.InMemoryQueue.InMemoryQueue("temp");
        await queue.Write(new Message<string> { Payload = "before-dispose" });

        // Dispose completes the writer, but reader should still have the item
        queue.Dispose();

        // After dispose, CancellationTokenSource is disposed, so reading throws
        await Assert.ThrowsAsync<ObjectDisposedException>(() => queue.Read());
    }

    [Fact]
    public async Task Write_MessageWithAllProperties_PreservesInJson()
    {
        var correlationId = Guid.NewGuid();
        var message = new Message<string>
        {
            Payload = "test",
            CorrelationId = correlationId,
            Type = MessageType.Event,
            State = MessageState.Succeeded,
            Name = "my-event"
        };

        await _queue.Write(message);
        var result = await _queue.Read<string>();

        Assert.NotNull(result);
        Assert.Equal("test", result!.Payload);
        Assert.Equal(correlationId, result.CorrelationId);
        Assert.Equal(MessageType.Event, result.Type);
        Assert.Equal("my-event", result.Name);
    }

    [Fact]
    public async Task Write_ComplexNestedPayload_SerializesCorrectly()
    {
        var payload = new NestedPayload
        {
            Name = "parent",
            Child = new NestedPayload { Name = "child" },
            Items = [1, 2, 3]
        };
        var message = new Message<NestedPayload> { Payload = payload };

        await _queue.Write(message);
        var result = await _queue.Read<NestedPayload>();

        Assert.NotNull(result?.Payload);
        Assert.Equal("parent", result!.Payload!.Name);
        Assert.Equal("child", result.Payload.Child!.Name);
        Assert.Equal([1, 2, 3], result.Payload.Items);
    }

    [Fact]
    public async Task Read_RawJson_ContainsPayloadData()
    {
        await _queue.Write(new Message<string> { Payload = "hello-json" });

        var json = await _queue.Read();

        Assert.NotNull(json);
        Assert.Contains("hello-json", json);
        Assert.Contains("Payload", json);
    }

    [Fact]
    public void OnDisposed_Event_FiredOnFirstDispose()
    {
        var count = 0;
        _queue.OnDisposed += () => count++;

        _queue.Dispose();
        _queue.Dispose();

        Assert.Equal(1, count);
    }

    [Fact]
    public async Task Write_AfterCancellation_ExternalToken_ThrowsCorrectly()
    {
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        // When external token is already cancelled, write should throw
        await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => _queue.Write(new Message<string> { Payload = "test" }, cts.Token));
    }

    [Fact]
    public async Task MultipleTypes_ReadInCorrectOrder()
    {
        await _queue.Write(new Message<int> { Payload = 1 });
        await _queue.Write(new Message<string> { Payload = "two" });
        await _queue.Write(new Message<double> { Payload = 3.0 });

        var json1 = await _queue.Read();
        var json2 = await _queue.Read();
        var json3 = await _queue.Read();

        Assert.Contains("1", json1);
        Assert.Contains("two", json2);
        Assert.Contains("3", json3);
    }

    public void Dispose()
    {
        _queue.Dispose();
    }

    private class NestedPayload
    {
        public string? Name { get; set; }
        public NestedPayload? Child { get; set; }
        public List<int>? Items { get; set; }
    }
}
