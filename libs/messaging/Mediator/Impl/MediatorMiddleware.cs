namespace Sencilla.Messaging.Mediator;

/// <summary>
/// In-process message handler middleware. Resolves and executes handlers via IMessageHandlerExecutor
/// for every type <see cref="MediatorConfig.ShouldHandle"/> admits — by default everything except
/// durable <c>[Stream]</c> commands, which the stream's consumer runs — then passes the message on.
/// </summary>
public class MediatorMiddleware(
    IServiceScopeFactory scopeFactory,
    IMessageHandlerExecutor executor,
    MediatorConfig config) : IMessageMiddleware
{
    public async Task HandleAsync<T>(Message<T> message, Func<Message<T>, CancellationToken, Task> next, CancellationToken cancellationToken = default)
    {
        if (config.ShouldHandle(typeof(T)))
        {
            using var scope = scopeFactory.CreateScope();
            await executor.ExecuteAsync(message, scope.ServiceProvider, cancellationToken);
        }

        await next(message, cancellationToken);
    }
}
