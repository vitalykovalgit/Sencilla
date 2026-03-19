namespace Sencilla.Messaging.Mediator;

/// <summary>
/// In-process message handler middleware. Resolves and executes handlers via IMessageHandlerExecutor.
/// Supports MediatorConfig filtering to allow/disable specific message types.
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
