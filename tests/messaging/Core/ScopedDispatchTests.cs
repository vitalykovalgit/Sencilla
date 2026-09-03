using System.Diagnostics;

namespace Sencilla.Messaging.Tests;

/// <summary>
/// The dispatcher is scoped so a scoped middleware sees the caller's scope; the pipeline shape is
/// still shared. Plus the envelope stamping the dispatcher owns: subject, initiator, trace parent.
/// </summary>
public class ScopedDispatchTests
{
    private class ScopedProbe : IMessageMiddleware
    {
        public int Calls;
        public Task HandleAsync<T>(Message<T> message, Func<Message<T>, CancellationToken, Task> next, CancellationToken token = default)
        {
            Calls++;
            return next(message, token);
        }
    }

    private class Capture : IMessageMiddleware
    {
        public Message? Last;
        public Task HandleAsync<T>(Message<T> message, Func<Message<T>, CancellationToken, Task> next, CancellationToken token = default)
        {
            Last = message;
            return next(message, token);
        }
    }

    private class Subject : IMessageHasEntity, IMessageHasUser
    {
        public Guid Entity { get; set; }
        public Guid User { get; set; }
        Guid? IMessageHasEntity.EntityId => Entity;
        Guid? IMessageHasUser.UserId => User;
    }

    [Fact]
    public async Task Dispatcher_IsScoped_AndResolvesScopedMiddlewareFromTheCallersScope()
    {
        var services = new ServiceCollection();
        services.AddSencillaMessaging(c => c.AddMiddlewareOnce<ScopedProbe>(ServiceLifetime.Scoped));
        using var sp = services.BuildServiceProvider();

        using var scope1 = sp.CreateScope();
        using var scope2 = sp.CreateScope();
        var d1 = scope1.ServiceProvider.GetRequiredService<IMessageDispatcher>();
        var d2 = scope2.ServiceProvider.GetRequiredService<IMessageDispatcher>();

        await d1.Send("a");
        await d1.Send("b");
        await d2.Send("c");

        Assert.Same(d1, scope1.ServiceProvider.GetRequiredService<IMessageDispatcher>());
        Assert.NotSame(d1, d2);

        var probe1 = scope1.ServiceProvider.GetRequiredService<ScopedProbe>();
        var probe2 = scope2.ServiceProvider.GetRequiredService<ScopedProbe>();
        Assert.NotSame(probe1, probe2);
        Assert.Equal(2, probe1.Calls);
        Assert.Equal(1, probe2.Calls);
    }

    [Fact]
    public async Task Send_StampsSubjectAndInitiator_FromThePayload()
    {
        var capture = new Capture();
        var services = new ServiceCollection();
        services.AddSingleton(capture);
        var config = new MessagingConfig();
        config.Middlewares.Add(typeof(Capture));
        using var sp = services.BuildServiceProvider();

        var subject = new Subject { Entity = Guid.NewGuid(), User = Guid.NewGuid() };
        await new MessageDispatcher(sp, config).Send(subject);

        Assert.Equal(subject.Entity, capture.Last!.EntityId);
        Assert.Equal(subject.User, capture.Last.UserId);
    }

    [Fact]
    public async Task Send_KeepsAnEnvelopeValueTheSenderSetExplicitly()
    {
        var capture = new Capture();
        var services = new ServiceCollection();
        services.AddSingleton(capture);
        var config = new MessagingConfig();
        config.Middlewares.Add(typeof(Capture));
        using var sp = services.BuildServiceProvider();

        var explicitEntity = Guid.NewGuid();
        await new MessageDispatcher(sp, config).Send(new Message<Subject>
        {
            EntityId = explicitEntity,
            Payload = new Subject { Entity = Guid.NewGuid() },
        });

        Assert.Equal(explicitEntity, capture.Last!.EntityId);
    }

    [Fact]
    public async Task Send_StampsTheCurrentTraceParent()
    {
        using var listener = new ActivityListener
        {
            ShouldListenTo = _ => true,
            Sample = (ref ActivityCreationOptions<ActivityContext> _) => ActivitySamplingResult.AllData,
        };
        ActivitySource.AddActivityListener(listener);
        using var source = new ActivitySource("ScopedDispatchTests");
        using var request = source.StartActivity("request");
        Assert.NotNull(request);

        var capture = new Capture();
        var services = new ServiceCollection();
        services.AddSingleton(capture);
        var config = new MessagingConfig();
        config.Middlewares.Add(typeof(Capture));
        using var sp = services.BuildServiceProvider();

        await new MessageDispatcher(sp, config).Send("traced");

        Assert.Equal(request!.Id, capture.Last!.Metadata![MessagingActivity.TraceParent]);
    }
}
