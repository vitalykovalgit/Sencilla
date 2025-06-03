
namespace Sencilla.Core
{
    /// <summary>
    /// Base command for all command implementation 
    /// </summary>
    public abstract class Command : ICommand
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="action"></param>
        /// <param name="priority"></param>
        public Command()
        {
            Id = Guid.NewGuid();
            CorrelationId = Guid.NewGuid();
        }

        /// <summary>
        /// </summary>
        /// <inheritdoc/>
        public Guid Id { get; }

        /// <summary>
        /// Correlation Id of command
        /// </summary>
        public Guid CorrelationId { get; set; }

    }

    /// <summary>
    /// 
    /// </summary>
    /// <inheritdoc/>
    /// <typeparam name="TResponse"></typeparam>
    public class Command<TResponse> : Command, ICommand<TResponse>
    {
    }
}
