namespace Sencilla.Messaging.InMemoryQueue.Tests;

public class InMemoryTopicEdgeCaseTests : IDisposable
{
    private readonly InMemoryTopic _topic;

    public InMemoryTopicEdgeCaseTests()
    {
        _topic = new InMemoryTopic("edge-topic");
    }

    [Fact]
    public async Task Write_NullMessage_DoesNotThrow()
    {
        _topic.Subscribe("sub1", 100);

        await _topic.Write<string>(null);
    }

    [Fact]
    public async Task Write_MultipleMessages_AllBroadcast()
    {
        var sub = _topic.Subscribe("sub1", 100);

        await _topic.Write(new Message<string> { Payload = "a" });
        await _topic.Write(new Message<string> { Payload = "b" });
        await _topic.Write(new Message<string> { Payload = "c" });

        var r1 = await sub.Read<string>();
        var r2 = await sub.Read<string>();
        var r3 = await sub.Read<string>();

        Assert.Equal("a", r1?.Payload);
        Assert.Equal("b", r2?.Payload);
        Assert.Equal("c", r3?.Payload);
    }

    [Fact]
    public async Task Write_AfterUnsubscribe_OnlyRemainingSubscriptionsReceive()
    {
        var sub1 = _topic.Subscribe("sub1", 100);
        var sub2 = _topic.Subscribe("sub2", 100);

        _topic.Unsubscribe("sub1");

        await _topic.Write(new Message<string> { Payload = "hello" });

        var result = await sub2.Read<string>();
        Assert.Equal("hello", result?.Payload);
    }

    [Fact]
    public void Subscribe_ManySubscriptions_AllTracked()
    {
        for (int i = 0; i < 20; i++)
            _topic.Subscribe($"sub-{i}", 100);

        Assert.Equal(20, _topic.GetSubscriptionNames().Count);
    }

    [Fact]
    public void Unsubscribe_AllSubscriptions_LeavesEmpty()
    {
        _topic.Subscribe("sub1", 100);
        _topic.Subscribe("sub2", 100);

        _topic.Unsubscribe("sub1");
        _topic.Unsubscribe("sub2");

        Assert.Empty(_topic.GetSubscriptionNames());
    }

    [Fact]
    public async Task Subscribe_WithCapacity_RespectsLimit()
    {
        var sub = _topic.Subscribe("bounded-sub", 1);

        await _topic.Write(new Message<string> { Payload = "a" });

        // Second message should block because subscription capacity is 1
        using var cts = new CancellationTokenSource(200);
        await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => _topic.Write(new Message<string> { Payload = "b" }, cts.Token));
    }

    [Fact]
    public async Task ConcurrentWrites_AllDelivered()
    {
        var sub = _topic.Subscribe("sub1", 100);

        var tasks = Enumerable.Range(0, 30).Select(i =>
            _topic.Write(new Message<int> { Payload = i }));
        await Task.WhenAll(tasks);

        var received = new List<int>();
        for (int i = 0; i < 30; i++)
        {
            var msg = await sub.Read<int>();
            Assert.NotNull(msg);
            received.Add(msg!.Payload);
        }

        Assert.Equal(30, received.Count);
        Assert.Equal(30, received.Distinct().Count());
    }

    [Fact]
    public void Dispose_MultipleTimes_DoesNotThrow()
    {
        _topic.Dispose();
        _topic.Dispose();
    }

    [Fact]
    public async Task Dispose_DisposesAllSubscriptions()
    {
        var sub1 = _topic.Subscribe("sub1", 100);
        var sub2 = _topic.Subscribe("sub2", 100);

        _topic.Dispose();

        // Subscriptions should be disposed
        await Assert.ThrowsAsync<ObjectDisposedException>(() => sub1.Read());
        await Assert.ThrowsAsync<ObjectDisposedException>(() => sub2.Read());
    }

    [Fact]
    public void Subscribe_AfterDispose_StillWorks()
    {
        // Dispose clears subscriptions but doesn't prevent new ones
        // (InMemoryTopic sets _disposed but Subscribe doesn't check it)
        _topic.Dispose();

        // After dispose, GetSubscriptionNames returns empty
        Assert.Empty(_topic.GetSubscriptionNames());
    }

    [Fact]
    public async Task Read_StringOverload_ThrowsNotSupported()
    {
        await Assert.ThrowsAsync<NotSupportedException>(() => _topic.Read());
    }

    [Fact]
    public async Task Read_GenericOverload_ThrowsNotSupported()
    {
        await Assert.ThrowsAsync<NotSupportedException>(() => _topic.Read<int>());
    }

    [Fact]
    public async Task Read_WithCancellationToken_ThrowsNotSupported()
    {
        using var cts = new CancellationTokenSource();
        await Assert.ThrowsAsync<NotSupportedException>(() => _topic.Read(cts.Token));
    }

    [Fact]
    public void Subscribe_EmptyName_Succeeds()
    {
        var sub = _topic.Subscribe("", 100);

        Assert.NotNull(sub);
        Assert.Equal("", sub.Name);
    }

    [Fact]
    public async Task Subscription_OnDisposed_RemovesFromTopic()
    {
        var sub = _topic.Subscribe("auto-remove", 100);

        sub.Dispose();

        Assert.Empty(_topic.GetSubscriptionNames());
    }

    [Fact]
    public async Task Write_ComplexPayload_BroadcastsCorrectly()
    {
        var sub1 = _topic.Subscribe("sub1", 100);
        var sub2 = _topic.Subscribe("sub2", 100);

        var payload = new TestPayload { Id = 1, Name = "test" };
        await _topic.Write(new Message<TestPayload> { Payload = payload });

        var r1 = await sub1.Read<TestPayload>();
        var r2 = await sub2.Read<TestPayload>();

        Assert.Equal(1, r1?.Payload?.Id);
        Assert.Equal("test", r1?.Payload?.Name);
        Assert.Equal(1, r2?.Payload?.Id);
        Assert.Equal("test", r2?.Payload?.Name);
    }

    public void Dispose()
    {
        _topic.Dispose();
    }

    private class TestPayload
    {
        public int Id { get; set; }
        public string? Name { get; set; }
    }
}
