namespace Sencilla.Core
{
    public interface IEvent
    {
        /// <summary>
        /// Identifier of the event 
        /// </summary>
        Guid Id { get; }

        /// <summary>
        /// Correlation Id 
        /// </summary>
        Guid CorrelationId { get; set; }

        /// <summary>
        /// Type of the event 
        /// </summary>
        string Type { get; }
    }
}
