
namespace Sencilla.Core
{
    public interface ICommandHandlerBase<in TCommand> where TCommand : ICommand
    {
    }

    /// <summary>
    /// Add response 
    /// </summary>
    /// <typeparam name="TCommand"></typeparam>
    public interface ICommandHandler<in TCommand>: ICommandHandlerBase<TCommand> where TCommand : ICommand
    {
        /// <summary>
        /// Handle command 
        /// </summary>
        /// <param name="cmd"></param>
        Task HandleAsync(TCommand cmd);
    }

    public interface ICommandHandlerBase<in TCommand, TResponse> where TCommand : class, ICommand<TResponse>
    {
    }

    /// <summary>
    /// 
    /// </summary>
    /// <typeparam name="TCommand"></typeparam>
    /// <typeparam name="TResponse"></typeparam>
    public interface ICommandHandler<in TCommand, TResponse>: ICommandHandlerBase<TCommand> where TCommand : class, ICommand<TResponse>
    {
        Task<TResponse> HandleAsync(TCommand command);
    }

}
