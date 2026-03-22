namespace Sencilla.Messaging.RabbitMQ.Tests;

public class RabbitMQStreamProviderTests
{
    [Fact]
    public void GetOrCreateStream_ReturnsStream()
    {
        var connectionFactory = new Mock<IRabbitMQConnectionFactory>();
        var config = new RabbitMQProviderConfig();
        var provider = new RabbitMQStreamProvider(connectionFactory.Object, config);

        var streamConfig = new StreamConfig(config) { Name = "test-queue" };
        var stream = provider.GetOrCreateStream(streamConfig);

        Assert.NotNull(stream);
        Assert.Equal("test-queue", stream.Name);
    }

    [Fact]
    public void GetOrCreateStream_SameName_ReturnsCached()
    {
        var connectionFactory = new Mock<IRabbitMQConnectionFactory>();
        var config = new RabbitMQProviderConfig();
        var provider = new RabbitMQStreamProvider(connectionFactory.Object, config);

        var streamConfig = new StreamConfig(config) { Name = "test-queue" };
        var stream1 = provider.GetOrCreateStream(streamConfig);
        var stream2 = provider.GetOrCreateStream(streamConfig);

        Assert.Same(stream1, stream2);
    }

    [Fact]
    public void GetOrCreateStream_DifferentNames_ReturnsDifferent()
    {
        var connectionFactory = new Mock<IRabbitMQConnectionFactory>();
        var config = new RabbitMQProviderConfig();
        var provider = new RabbitMQStreamProvider(connectionFactory.Object, config);

        var streamConfigA = new StreamConfig(config) { Name = "queue-a" };
        var streamConfigB = new StreamConfig(config) { Name = "queue-b" };
        var streamA = provider.GetOrCreateStream(streamConfigA);
        var streamB = provider.GetOrCreateStream(streamConfigB);

        Assert.NotSame(streamA, streamB);
        Assert.Equal("queue-a", streamA.Name);
        Assert.Equal("queue-b", streamB.Name);
    }

    [Fact]
    public void GetStream_NotFound_ReturnsNull()
    {
        var connectionFactory = new Mock<IRabbitMQConnectionFactory>();
        var config = new RabbitMQProviderConfig();
        var provider = new RabbitMQStreamProvider(connectionFactory.Object, config);

        var streamConfig = new StreamConfig(config) { Name = "nonexistent" };
        var stream = provider.GetStream(streamConfig);

        Assert.Null(stream);
    }

    [Fact]
    public void GetStream_NullName_ReturnsNull()
    {
        var connectionFactory = new Mock<IRabbitMQConnectionFactory>();
        var config = new RabbitMQProviderConfig();
        var provider = new RabbitMQStreamProvider(connectionFactory.Object, config);

        var streamConfig = new StreamConfig(config) { Name = null };
        var stream = provider.GetStream(streamConfig);

        Assert.Null(stream);
    }

    [Fact]
    public void GetOrCreateStream_NullName_ThrowsArgumentNullException()
    {
        var connectionFactory = new Mock<IRabbitMQConnectionFactory>();
        var config = new RabbitMQProviderConfig();
        var provider = new RabbitMQStreamProvider(connectionFactory.Object, config);

        var streamConfig = new StreamConfig(config) { Name = null };

        Assert.Throws<ArgumentNullException>(() => provider.GetOrCreateStream(streamConfig));
    }

    [Fact]
    public void GetStream_AfterCreate_ReturnsStream()
    {
        var connectionFactory = new Mock<IRabbitMQConnectionFactory>();
        var config = new RabbitMQProviderConfig();
        var provider = new RabbitMQStreamProvider(connectionFactory.Object, config);

        var streamConfig = new StreamConfig(config) { Name = "test-queue" };

        Assert.Null(provider.GetStream(streamConfig));

        var created = provider.GetOrCreateStream(streamConfig);
        var retrieved = provider.GetStream(streamConfig);

        Assert.Same(created, retrieved);
    }
}
