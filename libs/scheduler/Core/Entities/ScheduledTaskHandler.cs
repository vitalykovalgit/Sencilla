namespace Sencilla.Scheduler;

/// <summary>
/// 
/// </summary>
public class ScheduledTaskHandler
{
    /// <summary>
    /// 
    /// </summary>
    /// <value></value>
    public required Type HandlerType { get; init; }

    /// <summary>
    /// Method to call if any 
    /// </summary>
    /// <value></value>
    public MethodInfo? Method { get; set; }
}
