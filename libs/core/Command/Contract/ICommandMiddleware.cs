
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
        void Process(ICommand command);

    }
}
