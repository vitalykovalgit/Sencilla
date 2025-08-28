namespace Sencilla.Scheduler;

/// <summary>
/// Represents the status of a scheduled task.
/// </summary>
public enum ScheduledTaskStatus
{
    Idle,
    Queued,
    Pending,
    Running,
    Completed,
    Cancelled,
    Failed
}
