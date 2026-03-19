namespace Sencilla.Messaging.Tests;

public class BootstrapTests
{
    [Fact]
    public void AddSencillaMessaging_RegistersMessagingConfig()
    {
        var services = new ServiceCollection();

        services.AddSencillaMessaging(_ => { });

        var sp = services.BuildServiceProvider();
        var config = sp.GetRequiredService<MessagingConfig>();
        Assert.NotNull(config);
    }

    [Fact]
    public void AddSencillaMessaging_InvokesConfigAction()
    {
        var services = new ServiceCollection();
        var invoked = false;

        services.AddSencillaMessaging(_ => invoked = true);

        Assert.True(invoked);
    }

    [Fact]
    public void AddSencillaMessaging_CalledTwice_ReusesConfig()
    {
        var services = new ServiceCollection();

        services.AddSencillaMessaging(_ => { });
        services.AddSencillaMessaging(_ => { });

        var configDescriptors = services.Where(sd => sd.ServiceType == typeof(MessagingConfig)).ToList();
        Assert.Single(configDescriptors);
    }

    [Fact]
    public void AddSencillaMessaging_SecondCall_InvokesBothConfigs()
    {
        var services = new ServiceCollection();
        var callCount = 0;

        services.AddSencillaMessaging(_ => callCount++);
        services.AddSencillaMessaging(_ => callCount++);

        Assert.Equal(2, callCount);
    }

    [Fact]
    public void AddSencillaMessaging_ReturnsSameServiceCollection()
    {
        var services = new ServiceCollection();

        var result = services.AddSencillaMessaging(_ => { });

        Assert.Same(services, result);
    }

    [Fact]
    public void AddSencillaMessaging_ConfigCanRegisterMiddleware()
    {
        var services = new ServiceCollection();

        services.AddSencillaMessaging(config =>
        {
            config.AddMiddlewareOnce<TestMiddleware>();
        });

        var sp = services.BuildServiceProvider();
        var middleware = sp.GetService<TestMiddleware>();
        Assert.NotNull(middleware);
    }

    [Fact]
    public void AddSencillaMessaging_CleanupNullsServices()
    {
        var services = new ServiceCollection();

        services.AddSencillaMessaging(_ => { });

        var sp = services.BuildServiceProvider();
        var config = sp.GetRequiredService<MessagingConfig>();
        Assert.Null(config.Services);
    }

    [Fact]
    public void AddSencillaMessaging_RegistersMessageDispatcher()
    {
        var services = new ServiceCollection();
        services.AddLogging();

        services.AddSencillaMessaging(_ => { });

        var sp = services.BuildServiceProvider();
        var dispatcher = sp.GetRequiredService<IMessageDispatcher>();
        Assert.NotNull(dispatcher);
        Assert.IsType<MessageDispatcher>(dispatcher);
    }

    [Fact]
    public void AddSencillaMessaging_RegistersMessageDispatcherAsSingleton()
    {
        var services = new ServiceCollection();
        services.AddLogging();

        services.AddSencillaMessaging(_ => { });

        var sp = services.BuildServiceProvider();
        var first = sp.GetRequiredService<IMessageDispatcher>();
        var second = sp.GetRequiredService<IMessageDispatcher>();
        Assert.Same(first, second);
    }

    [Fact]
    public void AddSencillaMessaging_CalledTwice_RegistersDispatcherOnce()
    {
        var services = new ServiceCollection();
        services.AddLogging();

        services.AddSencillaMessaging(_ => { });
        services.AddSencillaMessaging(_ => { });

        var descriptors = services.Where(sd => sd.ServiceType == typeof(IMessageDispatcher)).ToList();
        Assert.Single(descriptors);
    }

    private class TestMiddleware : IMessageMiddleware
    {
        public Task HandleAsync<T>(Message<T> message, Func<Message<T>, CancellationToken, Task> next, CancellationToken cancellationToken = default)
            => next(message, cancellationToken);
    }
}
