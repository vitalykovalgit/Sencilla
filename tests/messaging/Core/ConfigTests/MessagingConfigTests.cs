namespace Sencilla.Messaging.Tests;

public class MessagingConfigTests
{
    private static MessagingConfig CreateConfig(ServiceCollection services)
    {
        MessagingConfig? captured = null;
        services.AddSencillaMessaging(c => captured = c);
        return captured!;
    }

    [Fact]
    public void Middlewares_InitiallyEmpty()
    {
        var config = new MessagingConfig();

        Assert.Empty(config.Middlewares);
    }

    [Fact]
    public void AppBuilders_InitiallyEmpty()
    {
        var config = new MessagingConfig();

        Assert.Empty(config.AppBuilders);
    }

    [Fact]
    public void AddMiddlewareOnce_RegistersMiddleware()
    {
        var services = new ServiceCollection();
        services.AddSencillaMessaging(config =>
        {
            config.AddMiddlewareOnce<TestMiddleware>();

            Assert.Single(config.Middlewares);
            Assert.Contains(typeof(TestMiddleware), config.Middlewares);
        });
    }

    [Fact]
    public void AddMiddlewareOnce_CalledTwice_RegistersOnlyOnce()
    {
        var services = new ServiceCollection();
        services.AddSencillaMessaging(config =>
        {
            config.AddMiddlewareOnce<TestMiddleware>();
            config.AddMiddlewareOnce<TestMiddleware>();

            Assert.Single(config.Middlewares);
        });
    }

    [Fact]
    public void AddMiddlewareOnce_ReturnsSelf_ForFluent()
    {
        var services = new ServiceCollection();
        services.AddSencillaMessaging(config =>
        {
            var result = config.AddMiddlewareOnce<TestMiddleware>();
            Assert.Same(config, result);
        });
    }

    [Fact]
    public void AddStreamProviderOnce_RegistersProvider()
    {
        var services = new ServiceCollection();
        services.AddSencillaMessaging(config =>
        {
            config.AddStreamProviderOnce<TestStreamProvider>();
            Assert.True(config.IsTypeRegistered<TestStreamProvider>());
        });
    }

    [Fact]
    public void AddStreamProviderOnce_CalledTwice_RegistersOnlyOnce()
    {
        var services = new ServiceCollection();
        services.AddSencillaMessaging(config =>
        {
            config.AddStreamProviderOnce<TestStreamProvider>();
            config.AddStreamProviderOnce<TestStreamProvider>();
        });

        var count = services.Count(sd => sd.ServiceType == typeof(TestStreamProvider));
        Assert.Equal(1, count);
    }

    [Fact]
    public void AddStreamProviderOnce_ReturnsSelf_ForFluent()
    {
        var services = new ServiceCollection();
        services.AddSencillaMessaging(config =>
        {
            var result = config.AddStreamProviderOnce<TestStreamProvider>();
            Assert.Same(config, result);
        });
    }

    [Fact]
    public void AddProviderConfigOnce_RegistersConfig()
    {
        var services = new ServiceCollection();
        services.AddSencillaMessaging(config =>
        {
            var providerConfig = config.AddProviderConfigOnce<TestProviderConfig>(null);
            Assert.NotNull(providerConfig);
        });
    }

    [Fact]
    public void AddProviderConfigOnce_InvokesAction()
    {
        var services = new ServiceCollection();
        var invoked = false;
        services.AddSencillaMessaging(config =>
        {
            config.AddProviderConfigOnce<TestProviderConfig>(c => invoked = true);
        });

        Assert.True(invoked);
    }

    [Fact]
    public void AddProviderConfigOnce_CalledTwice_ReturnsSameInstance()
    {
        var services = new ServiceCollection();
        services.AddSencillaMessaging(config =>
        {
            var first = config.AddProviderConfigOnce<TestProviderConfig>(null);
            var second = config.AddProviderConfigOnce<TestProviderConfig>(null);
            Assert.Same(first, second);
        });
    }

    [Fact]
    public void GetProviderConfig_ReturnsRegistered()
    {
        var services = new ServiceCollection();
        services.AddSencillaMessaging(config =>
        {
            config.AddProviderConfigOnce<TestProviderConfig>(null);
            var result = config.GetProviderConfig<TestProviderConfig>();
            Assert.NotNull(result);
        });
    }

    [Fact]
    public void GetProviderConfig_NotRegistered_ReturnsNull()
    {
        var services = new ServiceCollection();
        services.AddSencillaMessaging(config =>
        {
            var result = config.GetProviderConfig<TestProviderConfig>();
            Assert.Null(result);
        });
    }

    [Fact]
    public void IsTypeRegistered_Registered_ReturnsTrue()
    {
        var services = new ServiceCollection();
        services.AddSencillaMessaging(config =>
        {
            config.AddStreamProviderOnce<TestStreamProvider>();
            Assert.True(config.IsTypeRegistered<TestStreamProvider>());
        });
    }

    [Fact]
    public void IsTypeRegistered_NotRegistered_ReturnsFalse()
    {
        var services = new ServiceCollection();
        services.AddSencillaMessaging(config =>
        {
            Assert.False(config.IsTypeRegistered<TestStreamProvider>());
        });
    }

    [Fact]
    public void IsTypeNotRegistered_NotRegistered_ReturnsTrue()
    {
        var services = new ServiceCollection();
        services.AddSencillaMessaging(config =>
        {
            Assert.True(config.IsTypeNotRegistered<TestStreamProvider>());
        });
    }

    [Fact]
    public void Services_AvailableDuringConfig()
    {
        var services = new ServiceCollection();
        services.AddSencillaMessaging(config =>
        {
            Assert.NotNull(config.Services);
            Assert.Same(services, config.Services);
        });
    }

    [Fact]
    public void Services_NullAfterConfig()
    {
        var services = new ServiceCollection();
        services.AddSencillaMessaging(_ => { });

        var sp = services.BuildServiceProvider();
        var config = sp.GetRequiredService<MessagingConfig>();
        Assert.Null(config.Services);
    }

    [Fact]
    public void AddAppBuilderOnce_AddsBuilder()
    {
        var config = new MessagingConfig();
        Action<Microsoft.AspNetCore.Builder.IApplicationBuilder?, Microsoft.Extensions.Hosting.IHost?> builder = (_, _) => { };

        config.AddAppBuilderOnce(builder);

        Assert.Single(config.AppBuilders);
    }

    [Fact]
    public void AddAppBuilderOnce_SameBuilderTwice_AddsOnlyOnce()
    {
        var config = new MessagingConfig();
        Action<Microsoft.AspNetCore.Builder.IApplicationBuilder?, Microsoft.Extensions.Hosting.IHost?> builder = (_, _) => { };

        config.AddAppBuilderOnce(builder);
        config.AddAppBuilderOnce(builder);

        Assert.Single(config.AppBuilders);
    }

    [Fact]
    public void AddAppBuilderOnce_ReturnsSelf_ForFluent()
    {
        var config = new MessagingConfig();

        var result = config.AddAppBuilderOnce((_, _) => { });

        Assert.Same(config, result);
    }

    private class TestMiddleware : IMessageMiddleware
    {
        public Task HandleAsync<T>(Message<T> message, Func<Message<T>, CancellationToken, Task> next, CancellationToken cancellationToken = default)
            => next(message, cancellationToken);
    }

    private class TestStreamProvider : IMessageStreamProvider
    {
        public IMessageStream? GetStream(StreamConfig config) => null;
        public IMessageStream GetOrCreateStream(StreamConfig config) => throw new NotImplementedException();
    }

    private class TestProviderConfig : ProviderConfig { }
}
