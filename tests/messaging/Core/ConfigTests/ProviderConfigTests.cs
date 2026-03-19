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
}
