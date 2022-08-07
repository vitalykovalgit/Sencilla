
namespace Sencilla.Core
{
    /// <summary>
    /// 
    /// </summary>
    public interface ICommand
    {
        /// <summary>
        /// Unique identifier of the command
        /// </summary>
        Guid Id { get; }

        /// <summary>
        /// 
        /// </summary>
        Guid CorrelationId { get; }

    }

    /// <summary>
    /// Base class for all commands 
    /// </summary>
    /// <typeparam name="TData">Type of the data/peyload for the command</typeparam>
    public interface ICommand<TResponse> : ICommand
    {
        /// <summary>
        /// Command data/payload 
        /// </summary>
        //TData Payload { get; set; }
    }
}
