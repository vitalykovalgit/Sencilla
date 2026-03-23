namespace Sencilla.Messaging.InMemoryQueue.Tests;

public class InMemoryQueueBootstrapTests
{
    [Fact]
    public void UseInMemoryQueue_RegistersMiddleware()
    {
        var services = new ServiceCollection();
        services.AddSencillaMessaging(config =>
        {
            config.UseInMemoryQueue(null);
            Assert.Contains(typeof(InMemoryQueueMiddleware), config.Middlewares);
        });
    }

    [Fact]
    public void UseInMemoryQueue_RegistersStreamProvider()
    {
        var services = new ServiceCollection();
        services.AddSencillaMessaging(config =>
        {
            config.UseInMemoryQueue(null);
            Assert.True(config.IsTypeRegistered<InMemoryStreamProvider>());
        });
    }

    [Fact]
    public void UseInMemoryQueue_RegistersProviderConfig()
    {
        var services = new ServiceCollection();
        services.AddSencillaMessaging(config =>
        {
            config.UseInMemoryQueue(null);
            var providerConfig = config.GetProviderConfig<InMemoryProviderConfig>();
            Assert.NotNull(providerConfig);
        });
    }

    [Fact]
    public void UseInMemoryQueue_WithConfig_InvokesAction()
    {
        var services = new ServiceCollection();
        var invoked = false;
        services.AddSencillaMessaging(config =>
        {
            config.UseInMemoryQueue(c => invoked = true);
        });
        Assert.True(invoked);
    }

    [Fact]
    public void UseInMemoryQueue_CalledTwice_RegistersOnlyOnce()
    {
        var services = new ServiceCollection();
        services.AddSencillaMessaging(config =>
        {
            config.UseInMemoryQueue(null);
            config.UseInMemoryQueue(null);
            Assert.Single(config.Middlewares, m => m == typeof(InMemoryQueueMiddleware));
        });
    }

    [Fact]
    public void UseInMemoryQueue_ReturnsSameConfig_ForFluent()
    {
        var services = new ServiceCollection();
        services.AddSencillaMessaging(config =>
        {
            var result = config.UseInMemoryQueue(null);
            Assert.Same(config, result);
        });
    }

    [Fact]
    public void InMemoryProviderConfig_WithOptions_SetsOptions()
    {
        var providerConfig = new InMemoryProviderConfig();
        var invoked = false;

        var result = providerConfig.WithOptions(o => invoked = true);

        Assert.True(invoked);
        Assert.Same(providerConfig, result);
    }

    [Fact]
    public void InMemoryProviderConfig_InheritsProviderConfig()
    {
        var providerConfig = new InMemoryProviderConfig();

        Assert.NotNull(providerConfig.Consumers);
        Assert.NotNull(providerConfig.Streams);
        Assert.NotNull(providerConfig.Routes);
    }

    // ── Fluent API with InMemoryQueue provider ──

    [Fact]
    public void UseInMemoryQueue_Route_ConfiguresRoutes()
    {
        var services = new ServiceCollection();
        services.AddSencillaMessaging(config =>
        {
            config.UseInMemoryQueue(c =>
            {
                c.Route(r => r.Message<TestMessage>().To("test-queue"));
            });

            var providerConfig = config.GetProviderConfig<InMemoryProviderConfig>()!;
            var streams = providerConfig.Routes.GetStreams<TestMessage>();
            Assert.Contains("test-queue", streams);
        });
    }

    [Fact]
    public void UseInMemoryQueue_Route_MultipleTypes()
    {
        var services = new ServiceCollection();
        services.AddSencillaMessaging(config =>
        {
            config.UseInMemoryQueue(c =>
            {
                c.Route(r => r.Messages(typeof(TestMessage), typeof(TestMessage2)).To("shared-queue"));
            });

            var providerConfig = config.GetProviderConfig<InMemoryProviderConfig>()!;
            Assert.Contains("shared-queue", providerConfig.Routes.GetStreams<TestMessage>());
            Assert.Contains("shared-queue", providerConfig.Routes.GetStreams<TestMessage2>());
        });
    }

    [Fact]
    public void UseInMemoryQueue_AddConsumerFor_Queue()
    {
        var services = new ServiceCollection();
        services.AddSencillaMessaging(config =>
        {
            config.UseInMemoryQueue(c =>
            {
                c.AddConsumerFor("my-queue", con => con.HandleOnly<TestMessage>());
            });

            var providerConfig = config.GetProviderConfig<InMemoryProviderConfig>()!;
            Assert.True(providerConfig.Consumers.HasAnyConsumers);
            var consumer = providerConfig.Consumers.GetConsumers().First();
            Assert.Equal("my-queue", consumer.StreamName);
            Assert.Contains(typeof(TestMessage), consumer.AllowedTypes);
        });
    }

    [Fact]
    public void UseInMemoryQueue_AddConsumerFor_MultipleQueues()
    {
        var services = new ServiceCollection();
        services.AddSencillaMessaging(config =>
        {
            config.UseInMemoryQueue(c =>
            {
                c.AddConsumerFor(new[] { "q1", "q2" }, con => con.HandleOnly<TestMessage>());
            });

            var providerConfig = config.GetProviderConfig<InMemoryProviderConfig>()!;
            var consumers = providerConfig.Consumers.GetConsumers().ToList();
            Assert.Equal(2, consumers.Count);
        });
    }

    [Fact]
    public void UseInMemoryQueue_AddConsumerFor_GenericType()
    {
        var services = new ServiceCollection();
        services.AddSencillaMessaging(config =>
        {
            config.UseInMemoryQueue(c =>
            {
                c.AddConsumerFor<TestMessage>("typed-queue");
            });

            var providerConfig = config.GetProviderConfig<InMemoryProviderConfig>()!;
            var consumer = providerConfig.Consumers.GetConsumers().First();
            Assert.Contains(typeof(TestMessage), consumer.AllowedTypes);
        });
    }

    [Fact]
    public void UseInMemoryQueue_DefineQueue_CreatesStream()
    {
        var services = new ServiceCollection();
        services.AddSencillaMessaging(config =>
        {
            config.UseInMemoryQueue(c =>
            {
                c.DefineQueue("my-queue", s => s.Durable = false);
            });

            var providerConfig = config.GetProviderConfig<InMemoryProviderConfig>()!;
            var stream = providerConfig.Streams.GetConfig("my-queue");
            Assert.NotNull(stream);
            Assert.False(stream!.Durable);
        });
    }

    [Fact]
    public void UseInMemoryQueue_ForQueue_IsAliasForDefineQueue()
    {
        var services = new ServiceCollection();
        services.AddSencillaMessaging(config =>
        {
            config.UseInMemoryQueue(c =>
            {
                c.ForQueue("alias-queue");
            });

            var providerConfig = config.GetProviderConfig<InMemoryProviderConfig>()!;
            Assert.NotNull(providerConfig.Streams.GetConfig("alias-queue"));
        });
    }

    [Fact]
    public void UseInMemoryQueue_ForQueues_CreatesMultiple()
    {
        var services = new ServiceCollection();
        services.AddSencillaMessaging(config =>
        {
            config.UseInMemoryQueue(c =>
            {
                c.ForQueues(new[] { "q1", "q2", "q3" });
            });

            var providerConfig = config.GetProviderConfig<InMemoryProviderConfig>()!;
            Assert.NotNull(providerConfig.Streams.GetConfig("q1"));
            Assert.NotNull(providerConfig.Streams.GetConfig("q2"));
            Assert.NotNull(providerConfig.Streams.GetConfig("q3"));
        });
    }

    [Fact]
    public void UseInMemoryQueue_DefineTopic_CreatesTopic()
    {
        var services = new ServiceCollection();
        services.AddSencillaMessaging(config =>
        {
            config.UseInMemoryQueue(c =>
            {
                c.DefineTopic("my-topic");
            });

            var providerConfig = config.GetProviderConfig<InMemoryProviderConfig>()!;
            var stream = providerConfig.Streams.GetConfig("my-topic");
            Assert.NotNull(stream);
            Assert.True(stream!.Topic);
        });
    }

    [Fact]
    public void UseInMemoryQueue_FullFluentChain()
    {
        var services = new ServiceCollection();
        services.AddSencillaMessaging(config =>
        {
            config.UseInMemoryQueue(c =>
            {
                c.Route(r =>
                    {
                        r.Message<TestMessage>().To("orders-queue");
                        r.Messages(typeof(TestMessage), typeof(TestMessage2)).To("audit-queue");
                    })
                    .DefineQueue("orders-queue", s => s.AutoCreate = true)
                    .DefineQueue("audit-queue")
                    .AddConsumerFor("orders-queue", con =>
                    {
                        con.HandleOnly<TestMessage>();
                        con.Process(p => p.ByType());
                    })
                    .AddConsumerFor("audit-queue");
            });

            var providerConfig = config.GetProviderConfig<InMemoryProviderConfig>()!;

            // Routes
            Assert.Contains("orders-queue", providerConfig.Routes.GetStreams<TestMessage>());
            Assert.Contains("audit-queue", providerConfig.Routes.GetStreams<TestMessage>());
            Assert.Contains("audit-queue", providerConfig.Routes.GetStreams<TestMessage2>());

            // Streams
            Assert.NotNull(providerConfig.Streams.GetConfig("orders-queue"));
            Assert.True(providerConfig.Streams.GetConfig("orders-queue")!.AutoCreate);

            // Consumers
            var consumers = providerConfig.Consumers.GetConsumers().ToList();
            Assert.Equal(2, consumers.Count);
            var ordersConsumer = consumers.First(c => c.StreamName == "orders-queue");
            Assert.Contains(typeof(TestMessage), ordersConsumer.AllowedTypes);
            Assert.NotNull(ordersConsumer.Processing);
            Assert.True(ordersConsumer.Processing!.ResolveByType);
        });
    }

    [Fact]
    public void UseInMemoryQueue_AutoStartConsumers_False_RegistersAsSingleton()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSencillaMessaging(config =>
        {
            config.UseInMemoryQueue(c =>
            {
                c.AutoStartConsumers = false;
                c.AddConsumerFor("test-queue");
            });
        });

        var hostedDescriptor = services
            .Any(sd => sd.ServiceType == typeof(Microsoft.Extensions.Hosting.IHostedService)
                    && sd.ImplementationType == typeof(MessageStreamsConsumer<InMemoryStreamProvider, InMemoryProviderConfig>));
        Assert.False(hostedDescriptor);
    }

    private record TestMessage(string Value);
    private record TestMessage2(string Value);
}
