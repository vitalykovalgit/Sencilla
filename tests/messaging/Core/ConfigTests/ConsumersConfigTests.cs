namespace Sencilla.Messaging.Tests;

public class ConsumersConfigTests
{
    private readonly ConsumersConfig _consumers = new();

    [Fact]
    public void HasAnyConsumers_InitiallyFalse()
    {
        Assert.False(_consumers.HasAnyConsumers);
    }

    [Fact]
    public void GetConsumers_InitiallyEmpty()
    {
        Assert.Empty(_consumers.GetConsumers());
    }

    [Fact]
    public void ForStream_AddsConsumer()
    {
        _consumers.ForStream("test-stream");

        Assert.True(_consumers.HasAnyConsumers);
        Assert.Single(_consumers.GetConsumers());
    }

    [Fact]
    public void ForStream_SetsStreamName()
    {
        _consumers.ForStream("my-stream");

        var consumer = _consumers.GetConsumers().First();
        Assert.Equal("my-stream", consumer.StreamName);
    }

    [Fact]
    public void ForStream_WithConfig_InvokesAction()
    {
        _consumers.ForStream("test", c => c.PrefetchCount = 10);

        var consumer = _consumers.GetConsumers().First();
        Assert.Equal(10, consumer.PrefetchCount);
    }

    [Fact]
    public void ForStream_SameNameTwice_ReturnsSameInstance()
    {
        _consumers.ForStream("test");
        _consumers.ForStream("test", c => c.MaxConcurrentHandlers = 5);

        Assert.Single(_consumers.GetConsumers());
        Assert.Equal(5, _consumers.GetConsumers().First().MaxConcurrentHandlers);
    }

    [Fact]
    public void ForStream_ReturnsSelf_ForFluent()
    {
        var result = _consumers.ForStream("test");

        Assert.Same(_consumers, result);
    }

    [Fact]
    public void ForQueue_SetsStreamName()
    {
        _consumers.ForQueue("my-queue");

        var consumer = _consumers.GetConsumers().First();
        Assert.Equal("my-queue", consumer.StreamName);
    }

    [Fact]
    public void ForQueue_WithConfig_InvokesAction()
    {
        _consumers.ForQueue("queue", c => c.AutoAck = false);

        var consumer = _consumers.GetConsumers().First();
        Assert.False(consumer.AutoAck);
    }

    [Fact]
    public void ForTopic_SetsStreamNameAndSubscription()
    {
        _consumers.ForTopic("my-topic", "my-sub");

        var consumer = _consumers.GetConsumers().First();
        Assert.Equal("my-topic", consumer.StreamName);
        Assert.Equal("my-sub", consumer.StreamSubscription);
    }

    [Fact]
    public void ForTopic_WithConfig_InvokesAction()
    {
        _consumers.ForTopic("topic", "sub", c => c.Exclusive = true);

        var consumer = _consumers.GetConsumers().First();
        Assert.True(consumer.Exclusive);
    }

    [Fact]
    public void WithOptions_SetsDefaultsForNewConsumers()
    {
        _consumers.WithOptions(c => c.PrefetchCount = 50);
        _consumers.ForStream("test");

        var consumer = _consumers.GetConsumers().First();
        Assert.Equal(50, consumer.PrefetchCount);
    }

    [Fact]
    public void WithOptions_ReturnsSelf_ForFluent()
    {
        var result = _consumers.WithOptions(_ => { });

        Assert.Same(_consumers, result);
    }

    [Fact]
    public void MultipleConsumers_AllTracked()
    {
        _consumers.ForStream("stream-a");
        _consumers.ForStream("stream-b");
        _consumers.ForQueue("queue-c");

        Assert.Equal(3, _consumers.GetConsumers().Count());
    }
}
