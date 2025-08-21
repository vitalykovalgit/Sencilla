
namespace Sencilla.Core;

/// <summary>
/// Dispatch command 
/// </summary>
public class CommandDispatcher(IResolver resolver) : Resolveable(resolver), ICommandDispatcher
{

    /// <summary>
    /// Resolve handler for <see cref="ICommandHandler"/> and process command 
    /// </summary>
    /// <inheritdoc/>
    /// <param name="command"></param>
    /// <returns></returns>
    public async Task SendAsync(ICommand command, CancellationToken cancellationToken = default)
    {
        // get method and inject parameters but skip first parameter 
        //var handler = R(typeof(ICommandHandlerBase<>).MakeGenericType(command.GetType()));
        //await (handler?.CallWithInjectAsync(nameof(ICommandHandler<ICommand>.HandleAsync), command) ?? Task.CompletedTask);
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
    public async Task<TResponse?> SendAsync<TResponse>(ICommand<TResponse> command, CancellationToken cancellationToken = default)
    {
        // var handler = R(typeof(ICommandHandlerBase<,>).MakeGenericType(command.GetType(), typeof(TResponse)));
        // if (handler == null)
             return default;
// 
        // var response = await handler.CallWithInjectAsync<TResponse>(nameof(ICommandHandler<ICommand>.HandleAsync), command);
        // return response;
    }
}
