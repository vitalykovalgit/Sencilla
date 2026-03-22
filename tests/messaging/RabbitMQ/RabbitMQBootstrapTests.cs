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
}
