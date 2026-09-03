namespace Sencilla.Messaging;

/// <summary>
/// Dispatches messages through a chain-of-responsibility pipeline of registered middlewares.
///
/// <para><b>Scoped, cheaply.</b> Middlewares are resolved from the caller's
/// <see cref="IServiceProvider"/> on every dispatch, so a scoped middleware — the durable EF queue,
/// which must write through the caller's DbContext to commit with the caller's transaction — sees
/// the same scope as the code that called <c>Send</c>. Singleton middlewares resolve from a scoped
/// provider just as well. The pipeline SHAPE (which middleware types, in which order) is built once
/// per message type per configuration and cached process-wide, so scoping costs one dictionary
/// lookup per middleware per dispatch and nothing more.</para>
/// </summary>
public class MessageDispatcher(IServiceProvider serviceProvider, MessagingConfig config) : IMessageDispatcher
{
    /// <summary>Keyed by configuration as well as type: static so every scoped instance shares it.</summary>
    private static readonly ConcurrentDictionary<(MessagingConfig Config, Type Payload), object> PipelineCache = [];

    public Task Send<T>(T payload, CancellationToken cancellationToken = default)
        => Send(new Message<T> { Payload = payload }, cancellationToken);

    public Task Send<T>(Message<T> message, CancellationToken cancellationToken = default)
    {
        Stamp(message);

        var pipeline = (Func<IServiceProvider, Message<T>, CancellationToken, Task>)PipelineCache.GetOrAdd(
            (config, typeof(T)),
            _ => BuildPipeline<T>());

        return pipeline(serviceProvider, message, cancellationToken);
    }

    /// <summary>
    /// The single stamping choke point. Every transport serializes what it is handed, and a message
    /// that crosses a process boundary without PayloadType cannot be dispatched on the other side —
    /// so stamp here, once, rather than in each transport. Subject and initiator come from the
    /// payload when it declares them, so a durable transport can index them without the sender
    /// building an envelope by hand; the trace parent lets a consumer in another process continue
    /// the caller's trace.
    /// </summary>
    private static void Stamp<T>(Message<T> message)
    {
        message.PayloadType ??= PayloadTypeRegistry.KeyOf<T>();
        message.Namespace ??= typeof(T).FullName;
        message.Name ??= typeof(T).Name;

        if (message.Payload is IMessageHasEntity subject)
            message.EntityId ??= subject.EntityId;
        if (message.Payload is IMessageHasUser initiator)
            message.UserId ??= initiator.UserId;

        if (Activity.Current?.Id is { } traceparent)
            (message.Metadata ??= [])[MessagingActivity.TraceParent] = traceparent;
    }

    private Func<IServiceProvider, Message<T>, CancellationToken, Task> BuildPipeline<T>()
    {
        Func<IServiceProvider, Message<T>, CancellationToken, Task> next = (_, _, _) => Task.CompletedTask;

        for (var i = config.Middlewares.Count - 1; i >= 0; i--)
        {
            var middlewareType = config.Middlewares[i];
            var current = next;
            next = (provider, msg, token) =>
            {
                token.ThrowIfCancellationRequested();

                var middleware = (IMessageMiddleware)provider.GetRequiredService(middlewareType);
                try
                {
                    return middleware.HandleAsync(msg, (m, t) => current(provider, m, t), token);
                }
                catch (OperationCanceledException)
                {
                    throw;
                }
                catch (Exception ex)
                {
                    provider.GetService<ILogger<MessageDispatcher>>()?.LogError(ex,
                        "Middleware {Middleware} failed processing message {MessageId}", middlewareType.Name, msg.Id);
                    throw;
                }
            };
        }

        return next;
    }
}
