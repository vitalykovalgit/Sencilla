
namespace Sencilla.Core;

/// <summary>
/// Dispatch command 
/// </summary>
public class CommandDispatcher(IServiceProvider provider): ICommandDispatcher
{

    /// <summary>
    /// Resolve handler for <see cref="ICommandHandler"/> and process command 
    /// </summary>
    /// <inheritdoc/>
    /// <param name="command"></param>
    /// <returns></returns>
    public Task SendAsync(ICommand command, CancellationToken token = default)
    {
        // get method and inject parameters but skip first parameter 
        var handler = provider.GetService(typeof(ICommandHandlerBase<>).MakeGenericType(command.GetType()));
        return provider.InvokeMethod(handler, nameof(ICommandHandler<ICommand>.HandleAsync), command, token) ?? Task.CompletedTask;
    }

    /// <summary>
    /// Resolve handler <see cref="ICommandHandler<>"/> that can return response
    /// and process command 
    /// </summary>
    /// <inheritdoc/>
    /// <typeparam name="TResponse"></typeparam>
    /// <param name="command"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    public async Task<TResponse?> SendAsync<TResponse>(ICommand<TResponse> command, CancellationToken token = default)
    {
        var handler = provider.GetService(typeof(ICommandHandlerBase<,>).MakeGenericType(command.GetType(), typeof(TResponse)));
        if (handler == null)
             return default;

        var response = await provider.InvokeMethod<TResponse>(handler, nameof(ICommandHandler<ICommand>.HandleAsync), command, token);
        return response;
    }
}
