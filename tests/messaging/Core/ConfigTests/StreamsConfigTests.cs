namespace Sencilla.Messaging.Tests;

public class StreamsConfigTests
{
    private readonly ProviderConfig _provider = new();
    private StreamsConfig Streams => _provider.Streams;

    [Fact]
    public void GetConfig_NonExistentStream_ReturnsNull()
    {
        var config = Streams.GetConfig("nonexistent");

        Assert.Null(config);
    }

    [Fact]
    public void AddStream_CreatesStream()
    {
        Streams.AddStream("test-stream");

        var config = Streams.GetConfig("test-stream");

        Assert.NotNull(config);
        Assert.Equal("test-stream", config!.Name);
    }

    [Fact]
    public void AddStream_ReturnsSelf_ForFluent()
    {
        var result = Streams.AddStream("test");

        Assert.Same(Streams, result);
    }

    [Fact]
    public void AddStream_WithConfig_InvokesAction()
    {
        Streams.AddStream("test-stream", c => c.Durable = false);

        var config = Streams.GetConfig("test-stream");

        Assert.NotNull(config);
        Assert.False(config!.Durable);
    }

    [Fact]
    public void AddStream_SameNameTwice_ReturnsSameInstance()
    {
        Streams.AddStream("test");
        Streams.AddStream("test", c => c.MaxQueueSize = 50);

        var config = Streams.GetConfig("test");
        Assert.Equal(50, config!.MaxQueueSize);
    }

    [Fact]
    public void AddStreams_CreatesMultipleStreams()
    {
        Streams.AddStreams(["stream-a", "stream-b", "stream-c"]);

        Assert.NotNull(Streams.GetConfig("stream-a"));
        Assert.NotNull(Streams.GetConfig("stream-b"));
        Assert.NotNull(Streams.GetConfig("stream-c"));
    }

    [Fact]
    public void AddStreams_WithConfig_AppliesConfigToAll()
    {
        Streams.AddStreams(["s1", "s2"], c => c.MaxQueueSize = 100);

        Assert.Equal(100, Streams.GetConfig("s1")!.MaxQueueSize);
        Assert.Equal(100, Streams.GetConfig("s2")!.MaxQueueSize);
    }

    [Fact]
    public void AddQueue_IsSameAsAddStream()
    {
        Streams.AddQueue("my-queue");

        var config = Streams.GetConfig("my-queue");

        Assert.NotNull(config);
        Assert.Equal("my-queue", config!.Name);
    }

    [Fact]
    public void AddQueues_CreatesMultipleQueues()
    {
        Streams.AddQueues(["q1", "q2"]);

        Assert.NotNull(Streams.GetConfig("q1"));
        Assert.NotNull(Streams.GetConfig("q2"));
    }

    [Fact]
    public void AddTopic_SetsTopicFlag()
    {
        Streams.AddTopic("my-topic");

        var config = Streams.GetConfig("my-topic");

        Assert.NotNull(config);
        Assert.True(config!.Topic);
    }

    [Fact]
    public void AddTopic_WithConfig_AppliesConfigAndSetsTopicFlag()
    {
        Streams.AddTopic("my-topic", c => c.MaxQueueSize = 10);

        var config = Streams.GetConfig("my-topic");
        Assert.True(config!.Topic);
        Assert.Equal(10, config.MaxQueueSize);
    }

    [Fact]
    public void AddTopics_SetsTopicFlagOnAll()
    {
        Streams.AddTopics(["t1", "t2"]);

        Assert.True(Streams.GetConfig("t1")!.Topic);
        Assert.True(Streams.GetConfig("t2")!.Topic);
    }

    [Fact]
    public void WithOptions_SetsDefaultOptions()
    {
        Streams.WithOptions(c => c.Durable = false);
        Streams.AddStream("test");

        var config = Streams.GetConfig("test");
        Assert.False(config!.Durable);
    }

    [Fact]
    public void WithOptions_ReturnsSelf_ForFluent()
    {
        var result = Streams.WithOptions(_ => { });

        Assert.Same(Streams, result);
    }
}
