namespace Sencilla.Scheduler;

public enum SchedulerTaskStatus
{
    Idle,
    Queued,
    Pending,
    Running,
    Completed,
    Cancelled,
    Failed
}
