namespace Sencilla.Messaging.InMemoryQueue.Tests;

public class InMemoryStreamProviderTests
{
    private readonly InMemoryStreamProvider Provider = new();

    [Fact]
    public void GetOrCreateStream_CreatesNewStream()
    {
        var config = new StreamConfig(new TestProviderConfig()) { Name = "test-stream" };

        var stream = Provider.GetOrCreateStream(config);

        Assert.NotNull(stream);
        Assert.Equal("test-stream", stream.Name);
    }

    [Fact]
    public void GetOrCreateStream_ReturnsSameInstance()
    {
        var config = new StreamConfig(new TestProviderConfig()) { Name = "test-stream" };

        var stream1 = Provider.GetOrCreateStream(config);
        var stream2 = Provider.GetOrCreateStream(config);

        Assert.Same(stream1, stream2);
    }

    [Fact]
    public void GetOrCreateStream_NullName_ThrowsArgumentNullException()
    {
        var config = new StreamConfig(new TestProviderConfig()) { Name = null };

        Assert.Throws<ArgumentNullException>(() => Provider.GetOrCreateStream(config));
    }

    [Fact]
    public void GetOrCreateStream_DifferentNames_ReturnsDifferentInstances()
    {
        var config1 = new StreamConfig(new TestProviderConfig()) { Name = "stream-1" };
        var config2 = new StreamConfig(new TestProviderConfig()) { Name = "stream-2" };

        var stream1 = Provider.GetOrCreateStream(config1);
        var stream2 = Provider.GetOrCreateStream(config2);

        Assert.NotSame(stream1, stream2);
    }

    [Fact]
    public void GetStream_ReturnsNull_WhenStreamNotCreated()
    {
        var config = new StreamConfig(new TestProviderConfig()) { Name = "missing-stream" };

        var stream = Provider.GetStream(config);

        Assert.Null(stream);
    }

    [Fact]
    public void GetStream_ReturnsStream_WhenAlreadyCreated()
    {
        var config = new StreamConfig(new TestProviderConfig()) { Name = "test-stream" };

        var created = Provider.GetOrCreateStream(config);
        var retrieved = Provider.GetStream(config);

        Assert.NotNull(retrieved);
        Assert.Same(created, retrieved);
    }

    [Fact]
    public void GetStream_NullName_ReturnsNull()
    {
        var config = new StreamConfig(new TestProviderConfig()) { Name = null };

        var stream = Provider.GetStream(config);

        Assert.Null(stream);
    }

    [Fact]
    public void GetOrCreateStream_WithMaxQueueSize_CreatesStream()
    {
        var config = new StreamConfig(new TestProviderConfig()) { Name = "bounded", MaxQueueSize = 10 };

        var stream = Provider.GetOrCreateStream(config);

        Assert.NotNull(stream);
        Assert.Equal("bounded", stream.Name);
    }

    private class TestProviderConfig : ProviderConfig { }
}
