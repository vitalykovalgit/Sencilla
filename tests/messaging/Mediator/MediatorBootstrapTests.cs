namespace Sencilla.Messaging.Mediator.Tests;

public class MediatorBootstrapTests
{
    [Fact]
    public void UseMediator_RegistersMediatorMiddleware()
    {
        var services = new ServiceCollection();
        services.AddSencillaMessaging(config =>
        {
            config.UseMediator();
            Assert.Contains(typeof(MediatorMiddleware), config.Middlewares);
        });
    }

    [Fact]
    public void UseMediator_RegistersMediatorConfig()
    {
        var services = new ServiceCollection();
        services.AddSencillaMessaging(config =>
        {
            config.UseMediator();
            var mediatorConfig = config.GetProviderConfig<MediatorConfig>();
            Assert.NotNull(mediatorConfig);
        });
    }

    [Fact]
    public void UseMediator_WithConfig_InvokesAction()
    {
        var services = new ServiceCollection();
        var invoked = false;
        services.AddSencillaMessaging(config =>
        {
            config.UseMediator(c => invoked = true);
        });
        Assert.True(invoked);
    }

    [Fact]
    public void UseMediator_WithNullConfig_DoesNotThrow()
    {
        var services = new ServiceCollection();
        services.AddSencillaMessaging(config =>
        {
            config.UseMediator(null);
            Assert.Contains(typeof(MediatorMiddleware), config.Middlewares);
        });
    }

    [Fact]
    public void UseMediator_CalledTwice_RegistersOnlyOnce()
    {
        var services = new ServiceCollection();
        services.AddSencillaMessaging(config =>
        {
            config.UseMediator();
            config.UseMediator();
            Assert.Single(config.Middlewares, m => m == typeof(MediatorMiddleware));
        });
    }

    [Fact]
    public void UseMediator_ReturnsSameConfig_ForFluent()
    {
        var services = new ServiceCollection();
        services.AddSencillaMessaging(config =>
        {
            var result = config.UseMediator();
            Assert.Same(config, result);
        });
    }

    [Fact]
    public void UseMediator_CanChainWithOtherConfig()
    {
        var services = new ServiceCollection();
        services.AddSencillaMessaging(config =>
        {
            config.UseMediator()
                  .AddMiddlewareOnce<TestMiddleware>();
            Assert.Equal(2, config.Middlewares.Count);
        });
    }

    [Fact]
    public void MediatorConfig_InheritsProviderConfig()
    {
        var mediatorConfig = new MediatorConfig();

        Assert.NotNull(mediatorConfig.Consumers);
        Assert.NotNull(mediatorConfig.Streams);
        Assert.NotNull(mediatorConfig.Routes);
    }

    private class TestMiddleware : IMessageMiddleware
    {
        public Task ProcessAsync<T>(Message<T>? msg, CancellationToken cancellationToken = default)
            => Task.CompletedTask;
    }
}
