namespace Sencilla.Messaging.Tests;

public class StreamConfigTests
{
    private readonly ProviderConfig _provider = new();

    [Fact]
    public void Constructor_DefaultValues()
    {
        var config = new StreamConfig(_provider);

        Assert.Null(config.Name);
        Assert.Null(config.AutoCreate);
        Assert.Null(config.Topic);
        Assert.Null(config.MaxQueueSize);
        Assert.True(config.Durable);
    }

    [Fact]
    public void Constructor_WithClone_CopiesValues()
    {
        var original = new StreamConfig(_provider)
        {
            Name = "original",
            Topic = true,
            Durable = false,
            AutoCreate = true,
            MaxQueueSize = 500
        };

        var clone = new StreamConfig(_provider, original);

        Assert.Equal("original", clone.Name);
        Assert.True(clone.Topic);
        Assert.False(clone.Durable);
        Assert.True(clone.AutoCreate);
        Assert.Equal(500, clone.MaxQueueSize);
    }

    [Fact]
    public void Constructor_WithNullClone_UsesDefaults()
    {
        var config = new StreamConfig(_provider, null);

        Assert.Null(config.Name);
        Assert.True(config.Durable);
    }

    [Fact]
    public void Properties_CanBeSet()
    {
        var config = new StreamConfig(_provider)
        {
            Name = "test",
            AutoCreate = true,
            Topic = true,
            MaxQueueSize = 100,
            Durable = false
        };

        Assert.Equal("test", config.Name);
        Assert.True(config.AutoCreate);
        Assert.True(config.Topic);
        Assert.Equal(100, config.MaxQueueSize);
        Assert.False(config.Durable);
    }

    [Fact]
    public void Receive_AddsRouteForType()
    {
        var config = new StreamConfig(_provider) { Name = "test-stream" };

        config.Receive<string>();

        var streams = _provider.Routes.GetStreams<string>();
        Assert.Contains("test-stream", streams);
    }

    [Fact]
    public void Receive_ReturnsSelf_ForFluent()
    {
        var config = new StreamConfig(_provider) { Name = "test" };

        var result = config.Receive<string>();

        Assert.Same(config, result);
    }

    [Fact]
    public void For_Generic_AddsRouteForType()
    {
        var config = new StreamConfig(_provider) { Name = "test-stream" };

        config.For<int>();

        var streams = _provider.Routes.GetStreams<int>();
        Assert.Contains("test-stream", streams);
    }

    [Fact]
    public void For_MultipleTypes_AddsRoutesForAll()
    {
        var config = new StreamConfig(_provider) { Name = "multi-stream" };

        config.For(typeof(string), typeof(int));

        Assert.Contains("multi-stream", _provider.Routes.GetStreams<string>());
        Assert.Contains("multi-stream", _provider.Routes.GetStreams<int>());
    }

    [Fact]
    public void For_ReturnsSelf_ForFluent()
    {
        var config = new StreamConfig(_provider) { Name = "test" };

        var result = config.For<string>();

        Assert.Same(config, result);
    }

    [Fact]
    public void Receive_NullName_RoutesToEmptyString()
    {
        var config = new StreamConfig(_provider) { Name = null };

        config.Receive<string>();

        var streams = _provider.Routes.GetStreams<string>();
        Assert.Contains("", streams);
    }

    [Fact]
    public void AddConsumer_ReturnsSelf_ForFluent()
    {
        var config = new StreamConfig(_provider) { Name = "test" };

        var result = config.AddConsumer();

        Assert.Same(config, result);
    }

    [Fact]
    public void AddConsumer_WithName_RegistersConsumer()
    {
        var config = new StreamConfig(_provider) { Name = "consumer-stream" };

        config.AddConsumer();

        Assert.True(_provider.Consumers.HasAnyConsumers);
        var consumer = _provider.Consumers.GetConsumers().First();
        Assert.Equal("consumer-stream", consumer.StreamName);
    }

    [Fact]
    public void AddConsumer_WithConfig_AppliesConfig()
    {
        var config = new StreamConfig(_provider) { Name = "configured-stream" };

        config.AddConsumer(c =>
        {
            c.MaxConcurrentHandlers = 5;
            c.HandleOnly<string>();
        });

        var consumer = _provider.Consumers.GetConsumers().First();
        Assert.Equal(5, consumer.MaxConcurrentHandlers);
        Assert.Contains(typeof(string), consumer.AllowedTypes);
    }

    [Fact]
    public void AddConsumer_NullName_DoesNotRegister()
    {
        var config = new StreamConfig(_provider) { Name = null };

        config.AddConsumer();

        Assert.False(_provider.Consumers.HasAnyConsumers);
    }

    [Fact]
    public void AddConsumer_NullName_WithConfig_DoesNotRegister()
    {
        var config = new StreamConfig(_provider) { Name = null };

        config.AddConsumer(c => c.MaxConcurrentHandlers = 10);

        Assert.False(_provider.Consumers.HasAnyConsumers);
    }
}
