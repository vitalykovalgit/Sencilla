namespace Sencilla.Scheduler;

public class SchedulerTaskExecution
{
    public required string Task { get; init; }
    public DateTime StartDate { get; set; } = DateTime.UtcNow;
    public DateTime? CompleteDate { get; set; }
    public SchedulerTaskStatus Status { get; set; } = SchedulerTaskStatus.Running;
    public string? Error { get; set; }
    public int Attempt { get; set; } = 1;
}

