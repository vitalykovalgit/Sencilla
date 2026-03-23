namespace Sencilla.Messaging.Tests;

public class ProviderConfigTests
{
    [Fact]
    public void Constructor_InitializesAllProperties()
    {
        var config = new ProviderConfig();

        Assert.NotNull(config.Consumers);
        Assert.NotNull(config.Streams);
        Assert.NotNull(config.Routes);
    }

    [Fact]
    public void AddConsumers_InvokesConfigAction()
    {
        var config = new ProviderConfig();
        var invoked = false;

        config.AddConsumers(c => invoked = true);

        Assert.True(invoked);
    }

    [Fact]
    public void AddStreams_InvokesConfigAction()
    {
        var config = new ProviderConfig();
        var invoked = false;

        config.AddStreams(s => invoked = true);

        Assert.True(invoked);
    }

    [Fact]
    public void AddRoutes_InvokesConfigAction()
    {
        var config = new ProviderConfig();
        var invoked = false;

        config.AddRoutes(r => invoked = true);

        Assert.True(invoked);
    }

    [Fact]
    public void AddConsumers_ReturnsSelf_ForFluent()
    {
        var config = new ProviderConfig();

        var result = config.AddConsumers(_ => { });

        Assert.Same(config, result);
    }

    [Fact]
    public void AddStreams_ReturnsSelf_ForFluent()
    {
        var config = new ProviderConfig();

        var result = config.AddStreams(_ => { });

        Assert.Same(config, result);
    }

    [Fact]
    public void AddRoutes_ReturnsSelf_ForFluent()
    {
        var config = new ProviderConfig();

        var result = config.AddRoutes(_ => { });

        Assert.Same(config, result);
    }

    [Fact]
    public void AddConsumers_PassesConsumersConfig()
    {
        var config = new ProviderConfig();
        ConsumersConfig? captured = null;

        config.AddConsumers(c => captured = c);

        Assert.Same(config.Consumers, captured);
    }

    [Fact]
    public void AddStreams_PassesStreamsConfig()
    {
        var config = new ProviderConfig();
        StreamsConfig? captured = null;

        config.AddStreams(s => captured = s);

        Assert.Same(config.Streams, captured);
    }

    [Fact]
    public void AddRoutes_PassesRoutesConfig()
    {
        var config = new ProviderConfig();
        RoutesConfig? captured = null;

        config.AddRoutes(r => captured = r);

        Assert.Same(config.Routes, captured);
    }

    [Fact]
    public void AutoStartConsumers_DefaultsToTrue()
    {
        var config = new ProviderConfig();

        Assert.True(config.AutoStartConsumers);
    }

    [Fact]
    public void AutoStartConsumers_CanBeSetToFalse()
    {
        var config = new ProviderConfig();
        config.AutoStartConsumers = false;

        Assert.False(config.AutoStartConsumers);
    }

    // ── Route() alias ──

    [Fact]
    public void Route_IsAliasForAddRoutes()
    {
        var config = new ProviderConfig();
        RoutesConfig? captured = null;

        var result = config.Route(r => captured = r);

        Assert.Same(config.Routes, captured);
        Assert.Same(config, result);
    }

    // ── AddConsumerFor(string queue) ──

    [Fact]
    public void AddConsumerFor_SingleQueue_CreatesConsumer()
    {
        var config = new ProviderConfig();

        var result = config.AddConsumerFor("my-queue");

        Assert.True(config.Consumers.HasAnyConsumers);
        var consumer = config.Consumers.GetConsumers().First();
        Assert.Equal("my-queue", consumer.StreamName);
        Assert.Same(config, result);
    }

    [Fact]
    public void AddConsumerFor_SingleQueue_WithConfig_AppliesConfig()
    {
        var config = new ProviderConfig();

        config.AddConsumerFor("my-queue", c => c.MaxConcurrentHandlers = 5);

        var consumer = config.Consumers.GetConsumers().First();
        Assert.Equal(5, consumer.MaxConcurrentHandlers);
    }

    // ── AddConsumerFor<T>(string queue) ──

    [Fact]
    public void AddConsumerFor_Generic_RestrictsToType()
    {
        var config = new ProviderConfig();

        var result = config.AddConsumerFor<string>("typed-queue");

        var consumer = config.Consumers.GetConsumers().First();
        Assert.Equal("typed-queue", consumer.StreamName);
        Assert.Contains(typeof(string), consumer.AllowedTypes);
        Assert.Same(config, result);
    }

    // ── AddConsumerFor(string queue, Type[] types) ──

    [Fact]
    public void AddConsumerFor_WithTypes_RestrictsToTypes()
    {
        var config = new ProviderConfig();

        var result = config.AddConsumerFor("typed-queue", new[] { typeof(string), typeof(int) });

        var consumer = config.Consumers.GetConsumers().First();
        Assert.Contains(typeof(string), consumer.AllowedTypes);
        Assert.Contains(typeof(int), consumer.AllowedTypes);
        Assert.Same(config, result);
    }

    // ── AddConsumerFor(string queue, string[] namespaces) ──

    [Fact]
    public void AddConsumerFor_WithNamespaces_RestrictsToNamespaces()
    {
        var config = new ProviderConfig();

        var result = config.AddConsumerFor("ns-queue", new[] { "Sencilla.Orders", "Sencilla.Payments" });

        var consumer = config.Consumers.GetConsumers().First();
        Assert.Contains("Sencilla.Orders", consumer.AllowedNamespaces);
        Assert.Contains("Sencilla.Payments", consumer.AllowedNamespaces);
        Assert.Same(config, result);
    }

    // ── AddConsumerFor(string[] streams) ──

    [Fact]
    public void AddConsumerFor_MultipleQueues_CreatesMultipleConsumers()
    {
        var config = new ProviderConfig();

        var result = config.AddConsumerFor(new[] { "queue1", "queue2" });

        var consumers = config.Consumers.GetConsumers().ToList();
        Assert.Equal(2, consumers.Count);
        Assert.Same(config, result);
    }

    [Fact]
    public void AddConsumerFor_TopicSubscription_ParsesCorrectly()
    {
        var config = new ProviderConfig();

        config.AddConsumerFor(new[] { "my-topic:my-sub" });

        var consumer = config.Consumers.GetConsumers().First();
        Assert.Equal("my-topic", consumer.StreamName);
        Assert.Equal("my-sub", consumer.StreamSubscription);
    }

    [Fact]
    public void AddConsumerFor_MixedQueuesAndTopics_ParsesAll()
    {
        var config = new ProviderConfig();

        config.AddConsumerFor(new[] { "queue1", "topic1:sub1", "queue2" });

        var consumers = config.Consumers.GetConsumers().ToList();
        Assert.Equal(3, consumers.Count);

        var topicConsumer = consumers.First(c => c.StreamSubscription != null);
        Assert.Equal("topic1", topicConsumer.StreamName);
        Assert.Equal("sub1", topicConsumer.StreamSubscription);
    }

    [Fact]
    public void AddConsumerFor_TopicWithColonInName_SplitsOnlyFirstColon()
    {
        var config = new ProviderConfig();

        config.AddConsumerFor(new[] { "topic:sub:extra" });

        var consumer = config.Consumers.GetConsumers().First();
        Assert.Equal("topic", consumer.StreamName);
        Assert.Equal("sub:extra", consumer.StreamSubscription);
    }

    [Fact]
    public void AddConsumerFor_MultipleQueues_WithConfig_AppliesConfigToAll()
    {
        var config = new ProviderConfig();

        config.AddConsumerFor(new[] { "q1", "q2" }, c => c.MaxConcurrentHandlers = 10);

        var consumers = config.Consumers.GetConsumers().ToList();
        Assert.All(consumers, c => Assert.Equal(10, c.MaxConcurrentHandlers));
    }

    // ── DefineQueue / ForQueue ──

    [Fact]
    public void DefineQueue_AddsStreamConfig()
    {
        var config = new ProviderConfig();

        var result = config.DefineQueue("my-queue");

        var stream = config.Streams.GetConfig("my-queue");
        Assert.NotNull(stream);
        Assert.Equal("my-queue", stream!.Name);
        Assert.Same(config, result);
    }

    [Fact]
    public void DefineQueue_WithConfig_AppliesConfig()
    {
        var config = new ProviderConfig();

        config.DefineQueue("my-queue", s => s.Durable = false);

        var stream = config.Streams.GetConfig("my-queue");
        Assert.False(stream!.Durable);
    }

    [Fact]
    public void ForQueue_IsAliasForDefineQueue()
    {
        var config = new ProviderConfig();

        var result = config.ForQueue("alias-queue");

        var stream = config.Streams.GetConfig("alias-queue");
        Assert.NotNull(stream);
        Assert.Same(config, result);
    }

    // ── ForQueues ──

    [Fact]
    public void ForQueues_AddsMultipleQueues()
    {
        var config = new ProviderConfig();

        var result = config.ForQueues(new[] { "q1", "q2", "q3" });

        Assert.NotNull(config.Streams.GetConfig("q1"));
        Assert.NotNull(config.Streams.GetConfig("q2"));
        Assert.NotNull(config.Streams.GetConfig("q3"));
        Assert.Same(config, result);
    }

    [Fact]
    public void ForQueues_WithConfig_AppliesConfigToAll()
    {
        var config = new ProviderConfig();

        config.ForQueues(new[] { "q1", "q2" }, s => s.AutoCreate = true);

        Assert.True(config.Streams.GetConfig("q1")!.AutoCreate);
        Assert.True(config.Streams.GetConfig("q2")!.AutoCreate);
    }

    // ── DefineTopic ──

    [Fact]
    public void DefineTopic_AddsTopicStreamConfig()
    {
        var config = new ProviderConfig();

        var result = config.DefineTopic("my-topic");

        var stream = config.Streams.GetConfig("my-topic");
        Assert.NotNull(stream);
        Assert.True(stream!.Topic);
        Assert.Same(config, result);
    }

    [Fact]
    public void DefineTopic_WithConfig_AppliesConfig()
    {
        var config = new ProviderConfig();

        config.DefineTopic("my-topic", s => s.Durable = false);

        var stream = config.Streams.GetConfig("my-topic");
        Assert.True(stream!.Topic);
        Assert.False(stream.Durable);
    }
}
