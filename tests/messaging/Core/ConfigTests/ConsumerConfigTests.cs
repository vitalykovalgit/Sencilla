namespace Sencilla.Messaging.Tests;

public class ConsumerConfigTests
{
    [Fact]
    public void DefaultValues_AreCorrect()
    {
        var config = new ConsumerConfig();

        Assert.Null(config.StreamName);
        Assert.Null(config.StreamSubscription);
        Assert.Equal(1, config.MaxConcurrentHandlers);
        Assert.False(config.AsyncHandlers);
        Assert.Equal(1, config.PrefetchCount);
        Assert.True(config.AutoAck);
        Assert.Equal(1000, config.PoolingIntervalInMs);
        Assert.False(config.Exclusive);
    }

    [Fact]
    public void Constructor_WithClone_CopiesAllValues()
    {
        var original = new ConsumerConfig
        {
            StreamName = "original-stream",
            StreamSubscription = "my-sub",
            MaxConcurrentHandlers = 5,
            AsyncHandlers = true,
            PrefetchCount = 10,
            AutoAck = false,
            PoolingIntervalInMs = 2000,
            Exclusive = true
        };

        var clone = new ConsumerConfig(original);

        Assert.Equal("original-stream", clone.StreamName);
        Assert.Equal("my-sub", clone.StreamSubscription);
        Assert.Equal(5, clone.MaxConcurrentHandlers);
        Assert.Equal(10, clone.PrefetchCount);
        Assert.False(clone.AutoAck);
        Assert.Equal(2000, clone.PoolingIntervalInMs);
        Assert.True(clone.Exclusive);
    }

    [Fact]
    public void Constructor_WithNullClone_UsesDefaults()
    {
        var config = new ConsumerConfig(null);

        Assert.Equal(1, config.MaxConcurrentHandlers);
        Assert.True(config.AutoAck);
    }

    [Fact]
    public void Properties_CanBeModified()
    {
        var config = new ConsumerConfig
        {
            StreamName = "test",
            StreamSubscription = "sub",
            MaxConcurrentHandlers = 10,
            AsyncHandlers = true,
            PrefetchCount = 5,
            AutoAck = false,
            PoolingIntervalInMs = 500,
            Exclusive = true
        };

        Assert.Equal("test", config.StreamName);
        Assert.Equal("sub", config.StreamSubscription);
        Assert.Equal(10, config.MaxConcurrentHandlers);
        Assert.True(config.AsyncHandlers);
        Assert.Equal(5, config.PrefetchCount);
        Assert.False(config.AutoAck);
        Assert.Equal(500, config.PoolingIntervalInMs);
        Assert.True(config.Exclusive);
    }

    [Fact]
    public void Clone_IsIndependent_FromOriginal()
    {
        var original = new ConsumerConfig { PrefetchCount = 10 };
        var clone = new ConsumerConfig(original);

        original.PrefetchCount = 99;

        Assert.Equal(10, clone.PrefetchCount);
    }
}
