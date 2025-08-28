namespace Sencilla.Scheduler;

/// <summary>
/// Represents the execution of a scheduled task.
/// </summary>
public class ScheduledTaskExecution
{
    public Guid Id { get; } = Guid.NewGuid();
    public required ScheduledTask Task { get; init; }
    public DateTime StartDate { get; set; } = DateTime.UtcNow;
    public DateTime? CompleteDate { get; set; }
    public ScheduledTaskStatus Status { get; set; } = ScheduledTaskStatus.Running;
    public string? Error { get; set; }
    public int Attempt { get; set; } = 1;

    /// <summary>
    /// The last executed instance of the task.
    /// </summary>
    /// <value></value>
    public ScheduledTaskInstance? LastExecutedInstance { get; set; }

    /// <summary>
    /// The instances of the task execution.
    /// </summary>
    /// <value></value>
    public List<ScheduledTaskInstance> Instances { get; } = [];
}
