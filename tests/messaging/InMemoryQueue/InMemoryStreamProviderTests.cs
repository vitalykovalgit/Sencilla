namespace Sencilla.Messaging.InMemoryQueue.Tests;

public class InMemoryStreamProviderTests
{
    private readonly InMemoryStreamProvider _provider = new();

    [Fact]
    public void GetOrCreateStream_CreatesNewStream()
    {
        var config = new StreamConfig(new TestProviderConfig()) { Name = "test-stream" };

        var stream = _provider.GetOrCreateStream(config);

        Assert.NotNull(stream);
        Assert.Equal("test-stream", stream.Name);
    }

    [Fact]
    public void GetOrCreateStream_ReturnsSameInstance()
    {
        var config = new StreamConfig(new TestProviderConfig()) { Name = "test-stream" };

        var stream1 = _provider.GetOrCreateStream(config);
        var stream2 = _provider.GetOrCreateStream(config);

        Assert.Same(stream1, stream2);
    }

    [Fact]
    public void GetOrCreateStream_NullName_ThrowsArgumentNullException()
    {
        var config = new StreamConfig(new TestProviderConfig()) { Name = null };

        Assert.Throws<ArgumentNullException>(() => _provider.GetOrCreateStream(config));
    }

    [Fact]
    public void GetOrCreateStream_DifferentNames_ReturnsDifferentInstances()
    {
        var config1 = new StreamConfig(new TestProviderConfig()) { Name = "stream-1" };
        var config2 = new StreamConfig(new TestProviderConfig()) { Name = "stream-2" };

        var stream1 = _provider.GetOrCreateStream(config1);
        var stream2 = _provider.GetOrCreateStream(config2);

        Assert.NotSame(stream1, stream2);
    }

    [Fact]
    public void GetStream_ReturnsStream()
    {
        var config = new StreamConfig(new TestProviderConfig()) { Name = "test-stream" };

        // GetStream delegates to GetOrCreateStream
        var stream = _provider.GetStream(config);

        Assert.NotNull(stream);
        Assert.Equal("test-stream", stream!.Name);
    }

    [Fact]
    public void GetOrCreateStream_WithMaxQueueSize_CreatesStream()
    {
        var config = new StreamConfig(new TestProviderConfig()) { Name = "bounded", MaxQueueSize = 10 };

        var stream = _provider.GetOrCreateStream(config);

        Assert.NotNull(stream);
        Assert.Equal("bounded", stream.Name);
    }

    private class TestProviderConfig : ProviderConfig { }
}
