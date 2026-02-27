namespace Sencilla.Core.Tests;

/// <summary>
/// Tests for <see cref="IServiceProviderExt.GetMethodCached"/> and
/// <see cref="IServiceProviderExt.InvokeMethod"/>.
///
/// Covers: caching behavior, null handling, overload resolution, DI parameter injection.
/// </summary>
public class GetMethodCachedTests
{
    // ── Test helpers ──────────────────────────────────────────────────────────

    private class SampleHandler
    {
        public bool WasCalled { get; private set; }
        public string? LastArg { get; private set; }

        public Task HandleAsync(string message)
        {
            WasCalled = true;
            LastArg = message;
            return Task.CompletedTask;
        }

        public Task HandleAsync(int number)
        {
            WasCalled = true;
            LastArg = number.ToString();
            return Task.CompletedTask;
        }

        public Task<string> GetResultAsync(string input)
        {
            return Task.FromResult($"result:{input}");
        }

        public Task NoArgs()
        {
            WasCalled = true;
            return Task.CompletedTask;
        }
    }

    // ── GetMethodCached ──────────────────────────────────────────────────────

    [Fact]
    public void GetMethodCached_FindsMethodByName()
    {
        var handler = new SampleHandler();
        var method = handler.GetMethodCached("HandleAsync", new object[] { "hello" });

        Assert.NotNull(method);
        Assert.Equal("HandleAsync", method!.Name);
    }

    [Fact]
    public void GetMethodCached_ReturnsSameInstance_OnRepeatedCalls()
    {
        var handler = new SampleHandler();
        var args = new object[] { "test" };

        var first = handler.GetMethodCached("HandleAsync", args);
        var second = handler.GetMethodCached("HandleAsync", args);

        Assert.Same(first, second);
    }

    [Fact]
    public void GetMethodCached_DistinguishesOverloads_ByParamType()
    {
        var handler = new SampleHandler();

        var stringMethod = handler.GetMethodCached("HandleAsync", new object[] { "text" });
        var intMethod = handler.GetMethodCached("HandleAsync", new object[] { 42 });

        Assert.NotNull(stringMethod);
        Assert.NotNull(intMethod);
        Assert.NotSame(stringMethod, intMethod);

        // Verify correct overload was selected
        var stringParam = stringMethod!.GetParameters().First();
        var intParam = intMethod!.GetParameters().First();
        Assert.Equal(typeof(string), stringParam.ParameterType);
        Assert.Equal(typeof(int), intParam.ParameterType);
    }

    [Fact]
    public void GetMethodCached_ReturnsNull_WhenObjIsNull()
    {
        object? obj = null;
        var result = obj.GetMethodCached("Anything", []);

        Assert.Null(result);
    }

    [Fact]
    public void GetMethodCached_ReturnsNull_WhenMethodDoesNotExist()
    {
        var handler = new SampleHandler();
        var result = handler.GetMethodCached("NonExistentMethod", new object[] { "test" });

        Assert.Null(result);
    }

    [Fact]
    public void GetMethodCached_FindsMethodWithNoArgs()
    {
        var handler = new SampleHandler();
        var method = handler.GetMethodCached("NoArgs", []);

        Assert.NotNull(method);
        Assert.Equal("NoArgs", method!.Name);
        Assert.Empty(method.GetParameters());
    }

    // ── InvokeMethod ─────────────────────────────────────────────────────────

    [Fact]
    public async Task InvokeMethod_CallsCorrectMethod()
    {
        var handler = new SampleHandler();
        var sp = new ServiceCollection().BuildServiceProvider();

        await sp.InvokeMethod(handler, "HandleAsync", "hello");

        Assert.True(handler.WasCalled);
        Assert.Equal("hello", handler.LastArg);
    }

    [Fact]
    public async Task InvokeMethod_ReturnsCompletedTask_WhenObjIsNull()
    {
        var sp = new ServiceCollection().BuildServiceProvider();

        var task = sp.InvokeMethod(null, "HandleAsync", "test");
        await task;

        Assert.True(task.IsCompletedSuccessfully);
    }

    [Fact]
    public async Task InvokeMethodGeneric_ReturnsResult()
    {
        var handler = new SampleHandler();
        var sp = new ServiceCollection().BuildServiceProvider();

        var result = await sp.InvokeMethod<string>(handler, "GetResultAsync", "input");

        Assert.Equal("result:input", result);
    }

    [Fact]
    public async Task InvokeMethodGeneric_ReturnsDefault_WhenObjIsNull()
    {
        var sp = new ServiceCollection().BuildServiceProvider();

        var result = await sp.InvokeMethod<string>(null, "GetResultAsync", "test");

        Assert.Null(result);
    }

    // ── InjectMethodParameters ───────────────────────────────────────────────

    private class HandlerWithDependency
    {
        public string? ResolvedValue { get; private set; }

        public Task Handle(string input, ITestService service)
        {
            ResolvedValue = $"{input}:{service.GetValue()}";
            return Task.CompletedTask;
        }
    }

    public interface ITestService
    {
        string GetValue();
    }

    private class TestService : ITestService
    {
        public string GetValue() => "injected";
    }

    [Fact]
    public async Task InvokeMethod_InjectsDependencies_FromServiceProvider()
    {
        var handler = new HandlerWithDependency();
        var sp = new ServiceCollection()
            .AddSingleton<ITestService>(new TestService())
            .BuildServiceProvider();

        await sp.InvokeMethod(handler, "Handle", "data");

        Assert.Equal("data:injected", handler.ResolvedValue);
    }
}
