namespace Sencilla.Messaging.InMemoryQueue.Tests;

public class InMemoryTopicTests : IDisposable
{
    private readonly InMemoryTopic _topic;

    public InMemoryTopicTests()
    {
        _topic = new InMemoryTopic("test-topic");
    }

    [Fact]
    public void Name_ReturnsConstructorValue()
    {
        Assert.Equal("test-topic", _topic.Name);
    }

    [Fact]
    public async Task Read_ThrowsNotSupportedException()
    {
        await Assert.ThrowsAsync<NotSupportedException>(() => _topic.Read());
    }

    [Fact]
    public async Task ReadGeneric_ThrowsNotSupportedException()
    {
        await Assert.ThrowsAsync<NotSupportedException>(() => _topic.Read<string>());
    }

    [Fact]
    public void Subscribe_ReturnsQueue()
    {
        var sub = _topic.Subscribe("sub1", 100);

        Assert.NotNull(sub);
        Assert.Equal("sub1", sub.Name);
    }

    [Fact]
    public void Subscribe_DuplicateName_ThrowsInvalidOperation()
    {
        _topic.Subscribe("sub1", 100);

        Assert.Throws<InvalidOperationException>(() => _topic.Subscribe("sub1", 100));
    }

    [Fact]
    public async Task Write_BroadcastsToAllSubscriptions()
    {
        var sub1 = _topic.Subscribe("sub1", 100);
        var sub2 = _topic.Subscribe("sub2", 100);

        await _topic.Write(new Message<string> { Payload = "broadcast" });

        var r1 = await sub1.Read<string>();
        var r2 = await sub2.Read<string>();

        Assert.Equal("broadcast", r1?.Payload);
        Assert.Equal("broadcast", r2?.Payload);
    }

    [Fact]
    public async Task Write_NoSubscriptions_DoesNotThrow()
    {
        await _topic.Write(new Message<string> { Payload = "orphan" });
    }

    [Fact]
    public void Unsubscribe_ExistingSubscription_ReturnsTrue()
    {
        _topic.Subscribe("sub1", 100);

        var result = _topic.Unsubscribe("sub1");

        Assert.True(result);
    }

    [Fact]
    public void Unsubscribe_NonExistingSubscription_ReturnsFalse()
    {
        var result = _topic.Unsubscribe("nonexistent");

        Assert.False(result);
    }

    [Fact]
    public async Task Unsubscribe_StopsReceivingMessages()
    {
        var sub = _topic.Subscribe("sub1", 100);
        _topic.Unsubscribe("sub1");

        await _topic.Write(new Message<string> { Payload = "after-unsub" });

        // After unsubscribe, the subscription is disposed so reading throws ObjectDisposedException
        await Assert.ThrowsAsync<ObjectDisposedException>(
            () => sub.Read());
    }

    [Fact]
    public void GetSubscriptionNames_ReturnsAllNames()
    {
        _topic.Subscribe("sub1", 100);
        _topic.Subscribe("sub2", 100);
        _topic.Subscribe("sub3", 100);

        var names = _topic.GetSubscriptionNames();

        Assert.Equal(3, names.Count);
        Assert.Contains("sub1", names);
        Assert.Contains("sub2", names);
        Assert.Contains("sub3", names);
    }

    [Fact]
    public void GetSubscriptionNames_Empty_ReturnsEmptyList()
    {
        var names = _topic.GetSubscriptionNames();

        Assert.Empty(names);
    }

    [Fact]
    public void Dispose_ClearsAllSubscriptions()
    {
        _topic.Subscribe("sub1", 100);
        _topic.Subscribe("sub2", 100);

        _topic.Dispose();

        Assert.Empty(_topic.GetSubscriptionNames());
    }

    [Fact]
    public void SubscriptionDispose_RemovesFromTopic()
    {
        var sub = _topic.Subscribe("sub1", 100);

        sub.Dispose();

        Assert.Empty(_topic.GetSubscriptionNames());
    }

    public void Dispose()
    {
        _topic.Dispose();
    }
}
