namespace Sencilla.Messaging.InMemoryQueue.Tests;

public class InMemoryQueueTests : IDisposable
{
    private readonly Messaging.InMemoryQueue.InMemoryQueue _queue;

    public InMemoryQueueTests()
    {
        _queue = new Messaging.InMemoryQueue.InMemoryQueue("test-queue");
    }

    [Fact]
    public void Name_ReturnsConstructorValue()
    {
        Assert.Equal("test-queue", _queue.Name);
    }

    [Fact]
    public async Task Write_AndRead_Message()
    {
        var message = new Message<string> { Payload = "hello" };
        await _queue.Write(message);

        var result = await _queue.Read<string>();

        Assert.NotNull(result);
        Assert.Equal("hello", result!.Payload);
    }

    [Fact]
    public async Task Write_NullMessage_DoesNothing()
    {
        await _queue.Write<string>(null);

        using var cts = new CancellationTokenSource(100);
        var result = await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => _queue.Read(cts.Token));
    }

    [Fact]
    public async Task Write_NullPayload_DoesNothing()
    {
        await _queue.Write<string>((string?)null);

        using var cts = new CancellationTokenSource(100);
        await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => _queue.Read(cts.Token));
    }

    [Fact]
    public async Task Read_ReturnsJsonString()
    {
        var message = new Message<int> { Payload = 42 };
        await _queue.Write(message);

        var json = await _queue.Read();

        Assert.NotNull(json);
        Assert.Contains("42", json);
    }

    [Fact]
    public async Task Read_Generic_DeserializesCorrectly()
    {
        var payload = new TestData { Name = "test", Value = 99 };
        var message = new Message<TestData> { Payload = payload };
        await _queue.Write(message);

        var result = await _queue.Read<TestData>();

        Assert.NotNull(result);
        Assert.Equal("test", result!.Payload?.Name);
        Assert.Equal(99, result.Payload?.Value);
    }

    [Fact]
    public async Task Write_MultipleMessages_ReadInOrder()
    {
        await _queue.Write(new Message<string> { Payload = "first" });
        await _queue.Write(new Message<string> { Payload = "second" });
        await _queue.Write(new Message<string> { Payload = "third" });

        var r1 = await _queue.Read<string>();
        var r2 = await _queue.Read<string>();
        var r3 = await _queue.Read<string>();

        Assert.Equal("first", r1?.Payload);
        Assert.Equal("second", r2?.Payload);
        Assert.Equal("third", r3?.Payload);
    }

    [Fact]
    public async Task BoundedQueue_RespectsCapacity()
    {
        using var bounded = new Messaging.InMemoryQueue.InMemoryQueue("bounded", capacity: 2);

        await bounded.Write(new Message<string> { Payload = "a" });
        await bounded.Write(new Message<string> { Payload = "b" });

        // Third write should block since capacity is 2; use a timeout to verify
        using var cts = new CancellationTokenSource(200);
        await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => bounded.Write(new Message<string> { Payload = "c" }, cts.Token));
    }

    [Fact]
    public async Task Dispose_PreventsWriting()
    {
        _queue.Dispose();

        // After dispose, CancellationTokenSource is disposed so accessing Token throws
        await Assert.ThrowsAsync<ObjectDisposedException>(
            () => _queue.Write(new Message<string> { Payload = "test" }));
    }

    [Fact]
    public async Task Dispose_CancelsReading()
    {
        _queue.Dispose();

        await Assert.ThrowsAsync<ObjectDisposedException>(
            () => _queue.Read());
    }

    [Fact]
    public void Dispose_MultipleTimes_DoesNotThrow()
    {
        _queue.Dispose();
        _queue.Dispose();
    }

    [Fact]
    public void Dispose_InvokesOnDisposedEvent()
    {
        var invoked = false;
        _queue.OnDisposed += () => invoked = true;

        _queue.Dispose();

        Assert.True(invoked);
    }

    [Fact]
    public async Task Read_WithCancellation_ThrowsOperationCanceled()
    {
        using var cts = new CancellationTokenSource(100);

        await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => _queue.Read(cts.Token));
    }

    public void Dispose()
    {
        _queue.Dispose();
    }

    private class TestData
    {
        public string? Name { get; set; }
        public int Value { get; set; }
    }
}
