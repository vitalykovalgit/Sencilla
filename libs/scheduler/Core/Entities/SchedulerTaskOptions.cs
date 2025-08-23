namespace Sencilla.Scheduler;

public class SchedulerTaskOptions
{
    /// <summary>
    /// The name of the task.
    /// </summary>
    /// <value></value>
    public required string Name { get; init; }

    /// <summary>
    /// The description of the task.
    /// </summary>
    /// <value></value>
    public string? Desc { get; set; }

    /// <summary>
    /// The cron expression for the task.
    /// </summary>
    /// <value></value>
    public string? Cron { get; set; }

    /// <summary>
    /// The date and time when the task should be run.
    /// </summary>
    /// <value></value>
    public DateTime? RunAt { get; set; }

    public int? Retries { get; set; }
    public int[]? RetryIntervals { get; set; }
}

