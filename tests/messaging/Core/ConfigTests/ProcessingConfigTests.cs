namespace Sencilla.Messaging.Tests;

public class ProcessingConfigTests
{
    [Fact]
    public void DefaultValues_AreCorrect()
    {
        var config = new ProcessingConfig();

        Assert.True(config.ResolveByNamespace);
        Assert.False(config.ResolveByType);
    }

    [Fact]
    public void ByNamespace_SetsResolveByNamespace()
    {
        var config = new ProcessingConfig();

        var result = config.ByNamespace();

        Assert.True(config.ResolveByNamespace);
        Assert.Same(config, result);
    }

    [Fact]
    public void ByType_SetsResolveByType()
    {
        var config = new ProcessingConfig();

        var result = config.ByType();

        Assert.True(config.ResolveByType);
        Assert.Same(config, result);
    }

    [Fact]
    public void ByType_DoesNotAffectByNamespace()
    {
        var config = new ProcessingConfig();

        config.ByType();

        Assert.True(config.ResolveByNamespace);
        Assert.True(config.ResolveByType);
    }

    [Fact]
    public void FluentChaining_BothStrategies()
    {
        var config = new ProcessingConfig();

        var result = config.ByNamespace().ByType();

        Assert.Same(config, result);
        Assert.True(config.ResolveByNamespace);
        Assert.True(config.ResolveByType);
    }
}
