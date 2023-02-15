
namespace Sencilla.Core
{
    /// <summary>
    /// Publish command for processing
    /// Rename To Dispatcher 
    /// </summary>
    public interface ICommandDispatcher
    {
        /// <summary>
        /// Process command syncronysly 
        /// </summary>
        /// <typeparam name="TCommand"></typeparam>
        /// <param name="cmd"></param>
        /// <returns></returns>
        Task SendAsync(ICommand cmd, CancellationToken cancellationToken = default);

        /// <summary>
        /// 
        /// </summary>
        /// <typeparam name="TResponse"></typeparam>
        /// <param name="command"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        Task<TResponse> SendAsync<TResponse>(ICommand<TResponse> command, CancellationToken cancellationToken = default);

    }
}
