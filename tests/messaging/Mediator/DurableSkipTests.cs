namespace Sencilla.Messaging.Mediator.Tests;

/// <summary>
/// A [Stream] type is a durable command: the stream's consumer runs it, so the in-process mediator
/// must not — otherwise it executes twice — unless the host opts in with HandleDurable().
/// </summary>
public class DurableSkipTests
{
    [Stream("tasks")] public class DurableCommand;
    public class InlineCommand;

    private class Counter<T> : IMessageHandler<T>
    {
        public int Calls;
        public Task HandleAsync(T message, CancellationToken token) { Calls++; return Task.CompletedTask; }
    }

    private static (MediatorMiddleware Middleware, Counter<DurableCommand> Durable, Counter<InlineCommand> Inline) Build(MediatorConfig? config = null)
    {
        var durable = new Counter<DurableCommand>();
        var inline = new Counter<InlineCommand>();
        var services = new ServiceCollection();
        services.AddSingleton<IMessageHandler<DurableCommand>>(durable);
        services.AddSingleton<IMessageHandler<InlineCommand>>(inline);
        var sp = services.BuildServiceProvider();
        var middleware = new MediatorMiddleware(sp.GetRequiredService<IServiceScopeFactory>(), new MessageHandlerExecutor(), config ?? new MediatorConfig());
        return (middleware, durable, inline);
    }

    [Fact]
    public async Task StreamAttributedType_IsNotHandledInline_ButTheChainContinues()
    {
        var (middleware, durable, _) = Build();
        var continued = false;

        await middleware.HandleAsync(new Message<DurableCommand> { Payload = new() }, (_, _) => { continued = true; return Task.CompletedTask; });

        Assert.Equal(0, durable.Calls);
        Assert.True(continued);
    }

    [Fact]
    public async Task StreamAttributedType_IsHandledInline_WhenHandleDurable()
    {
        var (middleware, durable, _) = Build(new MediatorConfig().HandleDurable());

        await middleware.HandleAsync(new Message<DurableCommand> { Payload = new() }, (_, _) => Task.CompletedTask);

        Assert.Equal(1, durable.Calls);
    }

    [Fact]
    public async Task HandleDurable_StillHonoursTheDisableList()
    {
        var (middleware, durable, _) = Build(new MediatorConfig().HandleDurable().Disable<DurableCommand>());

        await middleware.HandleAsync(new Message<DurableCommand> { Payload = new() }, (_, _) => Task.CompletedTask);

        Assert.Equal(0, durable.Calls);
    }

    [Fact]
    public async Task PlainType_IsHandledInline()
    {
        var (middleware, _, inline) = Build();

        await middleware.HandleAsync(new Message<InlineCommand> { Payload = new() }, (_, _) => Task.CompletedTask);

        Assert.Equal(1, inline.Calls);
    }
}
