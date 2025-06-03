
namespace Sencilla.Core;

/// <summary>
/// Base event with default implementation 
/// </summary>
public class Event : IEvent
{
    /// <summary>
    /// 
    /// </summary>
    public Event()
    {
        Id = Guid.NewGuid();
        Type = GetType().Name;
    }

    /// <summary>
    /// 
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Correlation Id of event
    /// </summary>
    public Guid CorrelationId { get; set; }

    /// <summary>
    /// Return type of the event, by defult it usually name of the type
    /// but it can be overriden with deserealization process e.g. when we
    /// receive this event from message broker 
    /// </summary>
    public string Type { get; set; }
}

