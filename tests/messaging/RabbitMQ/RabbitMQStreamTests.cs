namespace Sencilla.Messaging.RabbitMQ.Tests;

public class RabbitMQStreamTests
{
    [Fact]
    public void Name_ReturnsStreamConfigName()
    {
        var connectionFactory = new Mock<IRabbitMQConnectionFactory>();
        var options = new RabbitMQProviderOptions();
        var providerConfig = new RabbitMQProviderConfig();
        var streamConfig = new StreamConfig(providerConfig) { Name = "orders" };

        var stream = new RabbitMQStream(connectionFactory.Object, streamConfig, options, null, NullLogger.Instance);

        Assert.Equal("orders", stream.Name);
    }

    [Fact]
    public void Constructor_NullName_ThrowsArgumentNullException()
    {
        var connectionFactory = new Mock<IRabbitMQConnectionFactory>();
        var options = new RabbitMQProviderOptions();
        var providerConfig = new RabbitMQProviderConfig();
        var streamConfig = new StreamConfig(providerConfig) { Name = null };

        Assert.Throws<ArgumentNullException>(() =>
            new RabbitMQStream(connectionFactory.Object, streamConfig, options, null, NullLogger.Instance));
    }

    [Fact]
    public async Task Write_NullMessage_DoesNothing()
    {
        var connectionFactory = new Mock<IRabbitMQConnectionFactory>();
        var options = new RabbitMQProviderOptions { AutoDeclareTopology = false };
        var providerConfig = new RabbitMQProviderConfig();
        var streamConfig = new StreamConfig(providerConfig) { Name = "test" };

        var stream = new RabbitMQStream(connectionFactory.Object, streamConfig, options, null, NullLogger.Instance);

        await stream.Write<string>(null);

        connectionFactory.Verify(f => f.CreateChannelAsync(), Times.Never);
    }

    [Fact]
    public async Task DisposeAsync_WithoutInit_DoesNotThrow()
    {
        var connectionFactory = new Mock<IRabbitMQConnectionFactory>();
        var options = new RabbitMQProviderOptions { AutoDeclareTopology = false };
        var providerConfig = new RabbitMQProviderConfig();
        var streamConfig = new StreamConfig(providerConfig) { Name = "test" };

        var stream = new RabbitMQStream(connectionFactory.Object, streamConfig, options, null, NullLogger.Instance);

        await stream.DisposeAsync();
    }

    [Fact]
    public void RabbitMQProviderOptions_Defaults()
    {
        var options = new RabbitMQProviderOptions();

        Assert.Equal("amqp://guest:guest@localhost:5672/", options.ConnectionString);
        Assert.Equal("", options.DefaultExchange);
        Assert.Equal((ushort)10, options.PrefetchCount);
        Assert.True(options.AutoDeclareTopology);
        Assert.True(options.EnableDeadLetterQueue);
        Assert.Equal("dlx", options.DeadLetterExchange);
        Assert.Equal("dlq", options.DeadLetterQueue);
        Assert.Equal(0, options.MessageTtlMilliseconds);
    }

    [Fact]
    public void RabbitMQProviderOptions_CanConfigure()
    {
        var options = new RabbitMQProviderOptions
        {
            ConnectionString = "amqp://user:pass@host:5672/vhost",
            DefaultExchange = "my-exchange",
            PrefetchCount = 50,
            AutoDeclareTopology = false,
            EnableDeadLetterQueue = false,
            DeadLetterExchange = "custom-dlx",
            DeadLetterQueue = "custom-dlq",
            MessageTtlMilliseconds = 30000
        };

        Assert.Equal("amqp://user:pass@host:5672/vhost", options.ConnectionString);
        Assert.Equal("my-exchange", options.DefaultExchange);
        Assert.Equal((ushort)50, options.PrefetchCount);
        Assert.False(options.AutoDeclareTopology);
        Assert.False(options.EnableDeadLetterQueue);
        Assert.Equal("custom-dlx", options.DeadLetterExchange);
        Assert.Equal("custom-dlq", options.DeadLetterQueue);
        Assert.Equal(30000, options.MessageTtlMilliseconds);
    }

    [Fact]
    public void Stream_ImplementsIMessageStream()
    {
        var connectionFactory = new Mock<IRabbitMQConnectionFactory>();
        var options = new RabbitMQProviderOptions();
        var providerConfig = new RabbitMQProviderConfig();
        var streamConfig = new StreamConfig(providerConfig) { Name = "test" };

        var stream = new RabbitMQStream(connectionFactory.Object, streamConfig, options, null, NullLogger.Instance);

        Assert.IsAssignableFrom<IMessageStream>(stream);
        Assert.IsAssignableFrom<IMessageStreamReader>(stream);
        Assert.IsAssignableFrom<IMessageStreamWriter>(stream);
        Assert.IsAssignableFrom<IAsyncDisposable>(stream);
    }

    // ── Acknowledgement ───────────────────────────────────────────────────────

    private static RabbitMQStream Stream(ConsumerConfig? consumer)
    {
        var providerConfig = new RabbitMQProviderConfig();
        return new RabbitMQStream(
            new Mock<IRabbitMQConnectionFactory>().Object,
            new StreamConfig(providerConfig) { Name = "orders" },
            new RabbitMQProviderOptions(),
            consumer,
            NullLogger.Instance);
    }

    [Fact]
    public void Stream_ImplementsIMessageStreamAck()
        => Assert.IsAssignableFrom<IMessageStreamAck>(Stream(null));

    /// <summary>
    /// Under autoAck the broker released the message when it dispatched it, so a failure can never be
    /// retried. Reporting it as terminal is what lets the consumer raise MessageFailed exactly once
    /// instead of waiting for a redelivery that will not come.
    /// </summary>
    [Fact]
    public async Task Nack_UnderAutoAck_IsTerminalWithoutTouchingTheBroker()
    {
        var stream = Stream(new ConsumerConfig { AutoAck = true });

        Assert.True(await stream.Nack(Guid.NewGuid(), "boom", retryable: true));
    }

    /// <summary>An id with no in-flight delivery cannot be rejected — say so rather than claim terminal.</summary>
    [Fact]
    public async Task Nack_UnknownMessage_IsNotTerminal()
    {
        var stream = Stream(new ConsumerConfig());

        Assert.False(await stream.Nack(Guid.NewGuid(), "boom", retryable: true));
    }

    [Fact]
    public async Task Ack_UnknownMessage_DoesNotThrow()
        => await Stream(new ConsumerConfig()).Ack(Guid.NewGuid());

    [Fact]
    public async Task Ack_UnderAutoAck_DoesNotThrow()
        => await Stream(new ConsumerConfig { AutoAck = true }).Ack(Guid.NewGuid());

    /// <summary>
    /// A producer-only stream is built with no consumer config at all; it must not decide it is in
    /// autoAck mode and silently swallow acknowledgements.
    /// </summary>
    [Fact]
    public async Task NoConsumerConfig_DefaultsToManualAck()
        => Assert.False(await Stream(null).Nack(Guid.NewGuid(), "boom", retryable: false));
}
