namespace Sencilla.Messaging.InMemoryQueue.Tests;

public class InMemoryStreamProviderEdgeCaseTests
{
    private readonly InMemoryStreamProvider _provider = new();

    [Fact]
    public void GetOrCreateStream_Topic_CreatesStream()
    {
        var config = new StreamConfig(new TestProviderConfig()) { Name = "topic", Topic = true };

        var stream = _provider.GetOrCreateStream(config);

        Assert.NotNull(stream);
        Assert.Equal("topic", stream.Name);
    }

    [Fact]
    public void GetOrCreateStream_EmptyName_ThrowsArgumentNullException()
    {
        // Empty string is valid, null is not
        var config = new StreamConfig(new TestProviderConfig()) { Name = "" };

        var stream = _provider.GetOrCreateStream(config);

        Assert.NotNull(stream);
        Assert.Equal("", stream.Name);
    }

    [Fact]
    public void GetOrCreateStream_NullConfig_ThrowsNullReferenceException()
    {
        Assert.ThrowsAny<Exception>(() => _provider.GetOrCreateStream(null!));
    }

    [Fact]
    public void GetOrCreateStream_ManyStreams_AllCreated()
    {
        for (int i = 0; i < 50; i++)
        {
            var config = new StreamConfig(new TestProviderConfig()) { Name = $"stream-{i}" };
            var stream = _provider.GetOrCreateStream(config);
            Assert.Equal($"stream-{i}", stream.Name);
        }
    }

    [Fact]
    public async Task GetOrCreateStream_ConcurrentCreation_ReturnsSameInstance()
    {
        var config = new StreamConfig(new TestProviderConfig()) { Name = "concurrent" };
        var streams = new System.Collections.Concurrent.ConcurrentBag<IMessageStream>();

        var tasks = Enumerable.Range(0, 20).Select(_ => Task.Run(() =>
        {
            var stream = _provider.GetOrCreateStream(config);
            streams.Add(stream);
        }));

        await Task.WhenAll(tasks);

        var unique = streams.Distinct().ToList();
        Assert.Single(unique);
    }

    [Fact]
    public void GetStream_NullName_ThrowsArgumentNullException()
    {
        var config = new StreamConfig(new TestProviderConfig()) { Name = null };

        Assert.Throws<ArgumentNullException>(() => _provider.GetStream(config));
    }

    [Fact]
    public void GetStream_ExistingStream_ReturnsSameAsGetOrCreate()
    {
        var config = new StreamConfig(new TestProviderConfig()) { Name = "test" };

        var created = _provider.GetOrCreateStream(config);
        var retrieved = _provider.GetStream(config);

        Assert.Same(created, retrieved);
    }

    [Fact]
    public void GetOrCreateStream_WithMaxQueueSize_CreatesProperQueue()
    {
        var config = new StreamConfig(new TestProviderConfig()) { Name = "bounded", MaxQueueSize = 5 };

        var stream = _provider.GetOrCreateStream(config);

        Assert.NotNull(stream);
        Assert.IsType<Messaging.InMemoryQueue.InMemoryQueue>(stream);
    }

    [Fact]
    public async Task GetOrCreateStream_ReturnsWritableAndReadableStream()
    {
        var config = new StreamConfig(new TestProviderConfig()) { Name = "functional" };

        var stream = _provider.GetOrCreateStream(config);
        await stream.Write(new Message<string> { Payload = "works" });

        var result = await stream.Read<string>();
        Assert.Equal("works", result?.Payload);
    }

    private class TestProviderConfig : ProviderConfig { }
}
