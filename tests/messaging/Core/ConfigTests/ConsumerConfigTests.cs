namespace Sencilla.Messaging.Tests;

public class ConsumerConfigTests
{
    [Fact]
    public void DefaultValues_AreCorrect()
    {
        var config = new ConsumerConfig();

        Assert.Null(config.StreamName);
        Assert.Null(config.StreamSubscription);
        Assert.Equal(1, config.MaxConcurrentHandlers);
        Assert.False(config.AsyncHandlers);
        Assert.Equal(1, config.PrefetchCount);
        Assert.True(config.AutoAck);
        Assert.Equal(1000, config.PoolingIntervalInMs);
        Assert.False(config.Exclusive);
    }

    [Fact]
    public void Constructor_WithClone_CopiesAllValues()
    {
        var original = new ConsumerConfig
        {
            StreamName = "original-stream",
            StreamSubscription = "my-sub",
            MaxConcurrentHandlers = 5,
            AsyncHandlers = true,
            PrefetchCount = 10,
            AutoAck = false,
            PoolingIntervalInMs = 2000,
            Exclusive = true
        };

        var clone = new ConsumerConfig(original);

        Assert.Equal("original-stream", clone.StreamName);
        Assert.Equal("my-sub", clone.StreamSubscription);
        Assert.Equal(5, clone.MaxConcurrentHandlers);
        Assert.Equal(10, clone.PrefetchCount);
        Assert.False(clone.AutoAck);
        Assert.Equal(2000, clone.PoolingIntervalInMs);
        Assert.True(clone.Exclusive);
    }

    [Fact]
    public void Constructor_WithNullClone_UsesDefaults()
    {
        var config = new ConsumerConfig(null);

        Assert.Equal(1, config.MaxConcurrentHandlers);
        Assert.True(config.AutoAck);
    }

    [Fact]
    public void Properties_CanBeModified()
    {
        var config = new ConsumerConfig
        {
            StreamName = "test",
            StreamSubscription = "sub",
            MaxConcurrentHandlers = 10,
            AsyncHandlers = true,
            PrefetchCount = 5,
            AutoAck = false,
            PoolingIntervalInMs = 500,
            Exclusive = true
        };

        Assert.Equal("test", config.StreamName);
        Assert.Equal("sub", config.StreamSubscription);
        Assert.Equal(10, config.MaxConcurrentHandlers);
        Assert.True(config.AsyncHandlers);
        Assert.Equal(5, config.PrefetchCount);
        Assert.False(config.AutoAck);
        Assert.Equal(500, config.PoolingIntervalInMs);
        Assert.True(config.Exclusive);
    }

    [Fact]
    public void Clone_IsIndependent_FromOriginal()
    {
        var original = new ConsumerConfig { PrefetchCount = 10 };
        var clone = new ConsumerConfig(original);

        original.PrefetchCount = 99;

        Assert.Equal(10, clone.PrefetchCount);
    }

    // ── New property defaults ──

    [Fact]
    public void DefaultValues_NewProperties_AreCorrect()
    {
        var config = new ConsumerConfig();

        Assert.Equal(0, config.MaxRetries);
        Assert.Equal(1, config.MaxConsumerPerQueue);
        Assert.True(config.LoadHandlersFromAssemblies);
        Assert.Null(config.CustomProcessorType);
        Assert.Null(config.Processing);
        Assert.Empty(config.AllowedTypes);
        Assert.Empty(config.AllowedNamespaces);
        Assert.False(config.HasFilters);
    }

    // ── HandleOnly<T>() ──

    [Fact]
    public void HandleOnly_Generic_AddsType()
    {
        var config = new ConsumerConfig();

        var result = config.HandleOnly<string>();

        Assert.Contains(typeof(string), config.AllowedTypes);
        Assert.Same(config, result);
    }

    [Fact]
    public void HandleOnly_Generic_MultipleCalls_AddsAllTypes()
    {
        var config = new ConsumerConfig();

        config.HandleOnly<string>().HandleOnly<int>();

        Assert.Contains(typeof(string), config.AllowedTypes);
        Assert.Contains(typeof(int), config.AllowedTypes);
        Assert.Equal(2, config.AllowedTypes.Count);
    }

    [Fact]
    public void HandleOnly_Generic_SameTypeTwice_NoDuplicate()
    {
        var config = new ConsumerConfig();

        config.HandleOnly<string>().HandleOnly<string>();

        Assert.Single(config.AllowedTypes);
    }

    // ── HandleOnly(params Type[]) ──

    [Fact]
    public void HandleOnly_Types_AddsAllTypes()
    {
        var config = new ConsumerConfig();

        var result = config.HandleOnly(typeof(string), typeof(int), typeof(double));

        Assert.Equal(3, config.AllowedTypes.Count);
        Assert.Contains(typeof(string), config.AllowedTypes);
        Assert.Contains(typeof(int), config.AllowedTypes);
        Assert.Contains(typeof(double), config.AllowedTypes);
        Assert.Same(config, result);
    }

    // ── HandleOnly(params string[]) ──

    [Fact]
    public void HandleOnly_Namespaces_AddsNamespaces()
    {
        var config = new ConsumerConfig();

        var result = config.HandleOnly("Sencilla.Orders", "Sencilla.Payments");

        Assert.Equal(2, config.AllowedNamespaces.Count);
        Assert.Contains("Sencilla.Orders", config.AllowedNamespaces);
        Assert.Contains("Sencilla.Payments", config.AllowedNamespaces);
        Assert.Same(config, result);
    }

    [Fact]
    public void HandleOnly_Namespaces_SameNamespaceTwice_NoDuplicate()
    {
        var config = new ConsumerConfig();

        config.HandleOnly("Sencilla.Orders", "Sencilla.Orders");

        Assert.Single(config.AllowedNamespaces);
    }

    // ── HasFilters ──

    [Fact]
    public void HasFilters_NoFilters_ReturnsFalse()
    {
        var config = new ConsumerConfig();

        Assert.False(config.HasFilters);
    }

    [Fact]
    public void HasFilters_WithTypeFilter_ReturnsTrue()
    {
        var config = new ConsumerConfig();
        config.HandleOnly<string>();

        Assert.True(config.HasFilters);
    }

    [Fact]
    public void HasFilters_WithNamespaceFilter_ReturnsTrue()
    {
        var config = new ConsumerConfig();
        config.HandleOnly("Sencilla.Orders");

        Assert.True(config.HasFilters);
    }

    // ── IsTypeAllowed ──

    [Fact]
    public void IsTypeAllowed_NoFilters_ReturnsTrue()
    {
        var config = new ConsumerConfig();

        Assert.True(config.IsTypeAllowed(typeof(string)));
        Assert.True(config.IsTypeAllowed(typeof(int)));
    }

    [Fact]
    public void IsTypeAllowed_TypeMatch_ReturnsTrue()
    {
        var config = new ConsumerConfig();
        config.HandleOnly<string>();

        Assert.True(config.IsTypeAllowed(typeof(string)));
    }

    [Fact]
    public void IsTypeAllowed_TypeNoMatch_ReturnsFalse()
    {
        var config = new ConsumerConfig();
        config.HandleOnly<string>();

        Assert.False(config.IsTypeAllowed(typeof(int)));
    }

    [Fact]
    public void IsTypeAllowed_NamespaceMatch_ReturnsTrue()
    {
        var config = new ConsumerConfig();
        config.HandleOnly("System");

        Assert.True(config.IsTypeAllowed(typeof(string)));
    }

    [Fact]
    public void IsTypeAllowed_NamespaceMatch_CaseInsensitive()
    {
        var config = new ConsumerConfig();
        config.HandleOnly("system");

        Assert.True(config.IsTypeAllowed(typeof(string)));
    }

    [Fact]
    public void IsTypeAllowed_NamespaceNoMatch_ReturnsFalse()
    {
        var config = new ConsumerConfig();
        config.HandleOnly("NonExistent.Namespace");

        Assert.False(config.IsTypeAllowed(typeof(string)));
    }

    [Fact]
    public void IsTypeAllowed_TypeMatchOverridesNamespaceMiss()
    {
        var config = new ConsumerConfig();
        config.HandleOnly<string>();
        config.HandleOnly("NonExistent.Namespace");

        Assert.True(config.IsTypeAllowed(typeof(string)));
    }

    // ── Process<TProcessor>() ──

    [Fact]
    public void Process_Generic_SetsCustomProcessorType()
    {
        var config = new ConsumerConfig();

        var result = config.Process<TestProcessor>();

        Assert.Equal(typeof(TestProcessor), config.CustomProcessorType);
        Assert.Same(config, result);
    }

    // ── Process(Action<ProcessingConfig>) ──

    [Fact]
    public void Process_Action_CreatesProcessingConfig()
    {
        var config = new ConsumerConfig();

        var result = config.Process(p => p.ByType());

        Assert.NotNull(config.Processing);
        Assert.True(config.Processing!.ResolveByType);
        Assert.Same(config, result);
    }

    [Fact]
    public void Process_Action_CalledTwice_ReusesSameInstance()
    {
        var config = new ConsumerConfig();

        config.Process(p => p.ByType());
        var first = config.Processing;
        config.Process(p => p.ByNamespace());
        var second = config.Processing;

        Assert.Same(first, second);
    }

    // ── LoadHandlersFromAssembly() ──

    [Fact]
    public void LoadHandlersFromAssembly_SetsFlag()
    {
        var config = new ConsumerConfig();
        config.LoadHandlersFromAssemblies = false;

        var result = config.LoadHandlersFromAssembly();

        Assert.True(config.LoadHandlersFromAssemblies);
        Assert.Same(config, result);
    }

    // ── WithOptions ──

    [Fact]
    public void WithOptions_InvokesAction_ReturnsSelf()
    {
        var config = new ConsumerConfig();

        var result = config.WithOptions(c =>
        {
            c.MaxConcurrentHandlers = 5;
            c.PrefetchCount = 10;
        });

        Assert.Equal(5, config.MaxConcurrentHandlers);
        Assert.Equal(10, config.PrefetchCount);
        Assert.Same(config, result);
    }

    // ── Clone constructor for new properties ──

    [Fact]
    public void Constructor_WithClone_CopiesNewProperties()
    {
        var original = new ConsumerConfig();
        original.MaxRetries = 3;
        original.MaxConsumerPerQueue = 5;
        original.LoadHandlersFromAssemblies = false;
        original.HandleOnly<string>();
        original.HandleOnly<int>();
        original.HandleOnly("Sencilla.Orders");
        original.Process<TestProcessor>();
        original.Process(p => p.ByType());

        var clone = new ConsumerConfig(original);

        Assert.Equal(3, clone.MaxRetries);
        Assert.Equal(5, clone.MaxConsumerPerQueue);
        Assert.False(clone.LoadHandlersFromAssemblies);
        Assert.Equal(typeof(TestProcessor), clone.CustomProcessorType);
        Assert.NotNull(clone.Processing);
        Assert.True(clone.Processing!.ResolveByType);
        Assert.Equal(2, clone.AllowedTypes.Count);
        Assert.Contains(typeof(string), clone.AllowedTypes);
        Assert.Contains(typeof(int), clone.AllowedTypes);
        Assert.Single(clone.AllowedNamespaces);
        Assert.Contains("Sencilla.Orders", clone.AllowedNamespaces);
    }

    [Fact]
    public void Clone_AllowedTypes_AreIndependent()
    {
        var original = new ConsumerConfig();
        original.HandleOnly<string>();

        var clone = new ConsumerConfig(original);
        original.HandleOnly<int>();

        Assert.Single(clone.AllowedTypes);
        Assert.DoesNotContain(typeof(int), clone.AllowedTypes);
    }

    [Fact]
    public void Clone_AllowedNamespaces_AreIndependent()
    {
        var original = new ConsumerConfig();
        original.HandleOnly("Sencilla.Orders");

        var clone = new ConsumerConfig(original);
        original.HandleOnly("Sencilla.Payments");

        Assert.Single(clone.AllowedNamespaces);
        Assert.DoesNotContain("Sencilla.Payments", clone.AllowedNamespaces);
    }

    [Fact]
    public void Clone_EmptyFilters_StaysEmpty()
    {
        var original = new ConsumerConfig();
        var clone = new ConsumerConfig(original);

        Assert.Empty(clone.AllowedTypes);
        Assert.Empty(clone.AllowedNamespaces);
    }

    [Fact]
    public void Clone_Processing_IsSharedReference()
    {
        var original = new ConsumerConfig();
        original.Process(p => p.ByType());

        var clone = new ConsumerConfig(original);

        Assert.Same(original.Processing, clone.Processing);
    }

    // ── Fluent chaining ──

    [Fact]
    public void FluentChaining_AllMethods()
    {
        var config = new ConsumerConfig();

        var result = config
            .HandleOnly<string>()
            .HandleOnly(typeof(int))
            .HandleOnly("Sencilla.Orders")
            .Process<TestProcessor>()
            .Process(p => p.ByType())
            .LoadHandlersFromAssembly()
            .WithOptions(c => c.MaxRetries = 3);

        Assert.Same(config, result);
        Assert.Contains(typeof(string), config.AllowedTypes);
        Assert.Contains(typeof(int), config.AllowedTypes);
        Assert.Contains("Sencilla.Orders", config.AllowedNamespaces);
        Assert.Equal(typeof(TestProcessor), config.CustomProcessorType);
        Assert.NotNull(config.Processing);
        Assert.True(config.LoadHandlersFromAssemblies);
        Assert.Equal(3, config.MaxRetries);
    }

    private class TestProcessor { }
}
