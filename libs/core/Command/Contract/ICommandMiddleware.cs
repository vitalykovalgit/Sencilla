
namespace Sencilla.Core
{
    /// <summary>
    /// 
    /// </summary>
    public interface ICommandMiddleware
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="command"></param>
        Task Process(ICommand command, CancellationToken token);

    }
}
