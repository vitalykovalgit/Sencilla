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
}
