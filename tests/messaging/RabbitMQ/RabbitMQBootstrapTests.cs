namespace Sencilla.Messaging.RabbitMQ.Tests;

public class RabbitMQBootstrapTests
{
    [Fact]
    public void UseRabbitMQ_RegistersMiddleware()
    {
        var services = new ServiceCollection();
        services.AddSencillaMessaging(config =>
        {
            config.UseRabbitMQ(null);
            Assert.Contains(typeof(RabbitMQMiddleware), config.Middlewares);
        });
    }

    [Fact]
    public void UseRabbitMQ_RegistersStreamProvider()
    {
        var services = new ServiceCollection();
        services.AddSencillaMessaging(config =>
        {
            config.UseRabbitMQ(null);
            Assert.True(config.IsTypeRegistered<RabbitMQStreamProvider>());
        });
    }

    [Fact]
    public void UseRabbitMQ_RegistersProviderConfig()
    {
        var services = new ServiceCollection();
        services.AddSencillaMessaging(config =>
        {
            config.UseRabbitMQ(null);
            var providerConfig = config.GetProviderConfig<RabbitMQProviderConfig>();
            Assert.NotNull(providerConfig);
        });
    }

    [Fact]
    public void UseRabbitMQ_WithConfig_InvokesAction()
    {
        var services = new ServiceCollection();
        var invoked = false;
        services.AddSencillaMessaging(config =>
        {
            config.UseRabbitMQ(c => invoked = true);
        });
        Assert.True(invoked);
    }

    [Fact]
    public void UseRabbitMQ_CalledTwice_RegistersOnlyOnce()
    {
        var services = new ServiceCollection();
        services.AddSencillaMessaging(config =>
        {
            config.UseRabbitMQ(null);
            config.UseRabbitMQ(null);
            Assert.Single(config.Middlewares, m => m == typeof(RabbitMQMiddleware));
        });
    }

    [Fact]
    public void UseRabbitMQ_ReturnsSameConfig_ForFluent()
    {
        var services = new ServiceCollection();
        services.AddSencillaMessaging(config =>
        {
            var result = config.UseRabbitMQ(null);
            Assert.Same(config, result);
        });
    }

    [Fact]
    public void RabbitMQProviderConfig_InheritsProviderConfig()
    {
        var providerConfig = new RabbitMQProviderConfig();

        Assert.NotNull(providerConfig.Consumers);
        Assert.NotNull(providerConfig.Streams);
        Assert.NotNull(providerConfig.Routes);
        Assert.NotNull(providerConfig.Options);
    }

    [Fact]
    public void RabbitMQProviderConfig_WithOptions_SetsOptions()
    {
        var providerConfig = new RabbitMQProviderConfig();
        var invoked = false;

        var result = providerConfig.WithOptions(o => invoked = true);

        Assert.True(invoked);
        Assert.Same(providerConfig, result);
    }

    [Fact]
    public void RabbitMQProviderConfig_WithOptions_ConfiguresConnectionString()
    {
        var providerConfig = new RabbitMQProviderConfig();
        providerConfig.WithOptions(o => o.ConnectionString = "amqp://custom:pass@host:5672/");

        Assert.Equal("amqp://custom:pass@host:5672/", providerConfig.Options.ConnectionString);
    }

    [Fact]
    public void UseRabbitMQ_RegistersConnectionFactory()
    {
        var services = new ServiceCollection();
        services.AddSencillaMessaging(config =>
        {
            config.UseRabbitMQ(null);
        });

        Assert.Contains(services, sd => sd.ServiceType == typeof(IRabbitMQConnectionFactory));
    }

    [Fact]
    public void UseRabbitMQ_RegistersTopologyManager()
    {
        var services = new ServiceCollection();
        services.AddSencillaMessaging(config =>
        {
            config.UseRabbitMQ(null);
        });

        Assert.Contains(services, sd => sd.ServiceType == typeof(IRabbitMQTopologyManager));
    }

    // ── Fluent API with RabbitMQ provider ──

    [Fact]
    public void UseRabbitMQ_Route_ConfiguresRoutes()
    {
        var services = new ServiceCollection();
        services.AddSencillaMessaging(config =>
        {
            config.UseRabbitMQ(c =>
            {
                c.Route(r => r.Message<TestMessage>().To("test-queue"));
            });

            var providerConfig = config.GetProviderConfig<RabbitMQProviderConfig>()!;
            var streams = providerConfig.Routes.GetStreams<TestMessage>();
            Assert.Contains("test-queue", streams);
        });
    }

    [Fact]
    public void UseRabbitMQ_Route_MultipleTypes()
    {
        var services = new ServiceCollection();
        services.AddSencillaMessaging(config =>
        {
            config.UseRabbitMQ(c =>
            {
                c.Route(r => r.Messages(typeof(TestMessage), typeof(TestMessage2)).To("shared-queue"));
            });

            var providerConfig = config.GetProviderConfig<RabbitMQProviderConfig>()!;
            Assert.Contains("shared-queue", providerConfig.Routes.GetStreams<TestMessage>());
            Assert.Contains("shared-queue", providerConfig.Routes.GetStreams<TestMessage2>());
        });
    }

    [Fact]
    public void UseRabbitMQ_AddConsumerFor_Queue()
    {
        var services = new ServiceCollection();
        services.AddSencillaMessaging(config =>
        {
            config.UseRabbitMQ(c =>
            {
                c.AddConsumerFor("my-queue", con => con.HandleOnly<TestMessage>());
            });

            var providerConfig = config.GetProviderConfig<RabbitMQProviderConfig>()!;
            Assert.True(providerConfig.Consumers.HasAnyConsumers);
            var consumer = providerConfig.Consumers.GetConsumers().First();
            Assert.Equal("my-queue", consumer.StreamName);
            Assert.Contains(typeof(TestMessage), consumer.AllowedTypes);
        });
    }

    [Fact]
    public void UseRabbitMQ_AddConsumerFor_TopicSubscription()
    {
        var services = new ServiceCollection();
        services.AddSencillaMessaging(config =>
        {
            config.UseRabbitMQ(c =>
            {
                c.AddConsumerFor(new[] { "events-topic:my-subscription" });
            });

            var providerConfig = config.GetProviderConfig<RabbitMQProviderConfig>()!;
            var consumer = providerConfig.Consumers.GetConsumers().First();
            Assert.Equal("events-topic", consumer.StreamName);
            Assert.Equal("my-subscription", consumer.StreamSubscription);
        });
    }

    [Fact]
    public void UseRabbitMQ_AddConsumerFor_GenericType()
    {
        var services = new ServiceCollection();
        services.AddSencillaMessaging(config =>
        {
            config.UseRabbitMQ(c =>
            {
                c.AddConsumerFor<TestMessage>("typed-queue");
            });

            var providerConfig = config.GetProviderConfig<RabbitMQProviderConfig>()!;
            var consumer = providerConfig.Consumers.GetConsumers().First();
            Assert.Contains(typeof(TestMessage), consumer.AllowedTypes);
        });
    }

    [Fact]
    public void UseRabbitMQ_AddConsumerFor_WithNamespaces()
    {
        var services = new ServiceCollection();
        services.AddSencillaMessaging(config =>
        {
            config.UseRabbitMQ(c =>
            {
                c.AddConsumerFor("ns-queue", new[] { "Sencilla.Orders", "Sencilla.Payments" });
            });

            var providerConfig = config.GetProviderConfig<RabbitMQProviderConfig>()!;
            var consumer = providerConfig.Consumers.GetConsumers().First();
            Assert.Contains("Sencilla.Orders", consumer.AllowedNamespaces);
            Assert.Contains("Sencilla.Payments", consumer.AllowedNamespaces);
        });
    }

    [Fact]
    public void UseRabbitMQ_DefineQueue_CreatesStream()
    {
        var services = new ServiceCollection();
        services.AddSencillaMessaging(config =>
        {
            config.UseRabbitMQ(c =>
            {
                c.DefineQueue("my-queue", s => s.Durable = false);
            });

            var providerConfig = config.GetProviderConfig<RabbitMQProviderConfig>()!;
            var stream = providerConfig.Streams.GetConfig("my-queue");
            Assert.NotNull(stream);
            Assert.False(stream!.Durable);
        });
    }

    [Fact]
    public void UseRabbitMQ_DefineTopic_CreatesTopic()
    {
        var services = new ServiceCollection();
        services.AddSencillaMessaging(config =>
        {
            config.UseRabbitMQ(c =>
            {
                c.DefineTopic("events-topic");
            });

            var providerConfig = config.GetProviderConfig<RabbitMQProviderConfig>()!;
            var stream = providerConfig.Streams.GetConfig("events-topic");
            Assert.NotNull(stream);
            Assert.True(stream!.Topic);
        });
    }

    [Fact]
    public void UseRabbitMQ_ForQueues_CreatesMultiple()
    {
        var services = new ServiceCollection();
        services.AddSencillaMessaging(config =>
        {
            config.UseRabbitMQ(c =>
            {
                c.ForQueues(new[] { "q1", "q2", "q3" });
            });

            var providerConfig = config.GetProviderConfig<RabbitMQProviderConfig>()!;
            Assert.NotNull(providerConfig.Streams.GetConfig("q1"));
            Assert.NotNull(providerConfig.Streams.GetConfig("q2"));
            Assert.NotNull(providerConfig.Streams.GetConfig("q3"));
        });
    }

    [Fact]
    public void UseRabbitMQ_FullFluentChain()
    {
        var services = new ServiceCollection();
        services.AddSencillaMessaging(config =>
        {
            config.UseRabbitMQ(c =>
            {
                c.WithOptions(o =>
                    {
                        o.ConnectionString = "amqp://user:pass@host:5672/";
                        o.PrefetchCount = 20;
                        o.EnableDeadLetterQueue = true;
                    })
                    .Route(r =>
                    {
                        r.Message<TestMessage>().To("orders-queue");
                        r.Messages(typeof(TestMessage), typeof(TestMessage2)).To("audit-queue");
                    })
                    .DefineQueue("orders-queue", s => s.AutoCreate = true)
                    .DefineQueue("audit-queue")
                    .DefineTopic("events-topic")
                    .AddConsumerFor("orders-queue", con =>
                    {
                        con.HandleOnly<TestMessage>();
                        con.Process(p => p.ByType());
                    })
                    .AddConsumerFor(new[] { "events-topic:sub1" });
            });

            var providerConfig = config.GetProviderConfig<RabbitMQProviderConfig>()!;

            // Options
            Assert.Equal("amqp://user:pass@host:5672/", providerConfig.Options.ConnectionString);
            Assert.Equal(20, providerConfig.Options.PrefetchCount);
            Assert.True(providerConfig.Options.EnableDeadLetterQueue);

            // Routes
            Assert.Contains("orders-queue", providerConfig.Routes.GetStreams<TestMessage>());
            Assert.Contains("audit-queue", providerConfig.Routes.GetStreams<TestMessage>());
            Assert.Contains("audit-queue", providerConfig.Routes.GetStreams<TestMessage2>());

            // Streams
            Assert.NotNull(providerConfig.Streams.GetConfig("orders-queue"));
            Assert.True(providerConfig.Streams.GetConfig("orders-queue")!.AutoCreate);
            Assert.True(providerConfig.Streams.GetConfig("events-topic")!.Topic);

            // Consumers
            var consumers = providerConfig.Consumers.GetConsumers().ToList();
            Assert.Equal(2, consumers.Count);
            var ordersConsumer = consumers.First(c => c.StreamName == "orders-queue");
            Assert.Contains(typeof(TestMessage), ordersConsumer.AllowedTypes);
            Assert.NotNull(ordersConsumer.Processing);
            var topicConsumer = consumers.First(c => c.StreamName == "events-topic");
            Assert.Equal("sub1", topicConsumer.StreamSubscription);
        });
    }

    [Fact]
    public void UseRabbitMQ_AutoStartConsumers_False_RegistersAsSingleton()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSencillaMessaging(config =>
        {
            config.UseRabbitMQ(c =>
            {
                c.AutoStartConsumers = false;
                c.AddConsumerFor("test-queue");
            });
        });

        var hostedDescriptor = services
            .Any(sd => sd.ServiceType == typeof(Microsoft.Extensions.Hosting.IHostedService)
                    && sd.ImplementationType == typeof(MessageStreamsConsumer<RabbitMQStreamProvider, RabbitMQProviderConfig>));
        Assert.False(hostedDescriptor);
    }

    private record TestMessage(string Value);
    private record TestMessage2(string Value);
}
