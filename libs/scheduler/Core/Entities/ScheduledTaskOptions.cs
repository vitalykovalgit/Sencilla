namespace Sencilla.Scheduler;

/// <summary>
/// Options for configuring a scheduled task.
/// </summary>
public class ScheduledTaskOptions
{
    /// <summary>
    /// The name of the task.
    /// </summary>
    /// <value></value>
    public required string Name { get; set; }

    /// <summary>
    /// The description of the task.
    /// </summary>
    public string? Desc { get; set; }

    /// <summary>
    /// The cron expression for the task.
    /// </summary>
    public string? Cron { get; set; }

    /// <summary>
    /// The interval at which the task should be run.
    /// </summary>
    public TimeSpan? Every { get; set; }

    /// <summary>
    /// The run behavior for the task.
    /// </summary>
    public bool RunImmediately { get; set; }

    /// <summary>
    /// The date and time when the task should be run.
    /// </summary>
    /// <value></value>
    public DateTime? RunAt { get; set; }

    /// <summary>
    /// The time span after which the task should be run.
    /// </summary>
    /// /// <value></value>
    public TimeSpan? RunIn { get; set; }

    /// <summary>
    /// The delay before the task should be run.
    /// </summary>
    /// <value></value>
    public TimeSpan? DelayFor { get; set; }

    /// <summary>
    /// 
    /// </summary>
    /// <value></value>
    public ulong? Repeat { get; set; }

    /// <summary>
    /// The number of times the task should be retried.
    /// </summary>
    /// <value></value>
    public ulong? Retry { get; set; }

    /// <summary>
    /// The intervals at which the task should be retried.
    /// </summary>
    /// <value></value> 
    public ulong[]? RetryIn { get; set; }

    /// <summary>
    /// The time zone in which the task should be run.
    /// </summary>
    /// <value></value>
    public string? TimeZone { get; set; }

    /// <summary>
    /// The date and time when the task should start.
    /// </summary>
    /// <value></value>
    public DateTime? From { get; set; }

    /// <summary>
    /// The date and time when the task should be run.
    /// </summary>
    /// <value></value>
    public DateTime? To { get; set; }

    /// <summary>
    /// The time span during which the task should be run.
    /// </summary>
    /// <value></value>
    public TimeSpan? RunDuring { get; set; }

    /// <summary>
    /// The data to be passed to the task.
    /// </summary>
    /// <value></value>
    public string? Data { get; set; }

    /// <summary>
    /// Indicates whether the task should be run in a separate thread.
    /// </summary>
    public bool RunInThread { get; set; }

    /// <summary>
    /// Indicates whether the task should be run synchronously.
    /// </summary>
    public bool RunInSynch { get; set; }

    /// <summary>
    /// 
    /// </summary>
    /// <value></value>
    public bool WaitUntilCompleted { get; set; }

    /// <summary>
    /// How many instance of handler to run 
    /// Default value is 1
    /// </summary>
    public int InstanceCount { get; set; } = 1;

    /// <summary>
    /// Gets or sets the batch for the scheduled task.
    /// </summary>
    /// <value></value>
    public string? Batch { get; set; }

    /// <summary>
    /// Gets or sets the tag for the scheduled task.
    /// </summary>
    /// <value></value>
    public string? Tag { get; set; }

    /// <summary>
    /// The cron expression for the task.
    /// </summary>
    private CronExpression? CronExpression;

    public CronExpression GetCronExpression()
    {
        var cron = Cron ?? ConvertToCron(Every);
        return CronExpression ??= CronExpression.Parse(cron, CronFormat.IncludeSeconds);
    }

    public TimeSpan GetNextOccurrence(DateTime from)
    {
        var next = GetCronExpression().GetNextOccurrence(from);
        return next.HasValue ? next.Value - from : TimeSpan.FromMicroseconds(1);
    }

    public ScheduledTaskOptions Merge(ScheduledTaskOptions other)
    {
        // Other has higher priority
        if (other is not null)
        {
            // Merge logic here
            // For example, you might want to combine Cron expressions or other properties
            if (!string.IsNullOrWhiteSpace(other.Cron)) Cron = other.Cron;
            if (other.RunAt.HasValue) RunAt = other.RunAt;

            if (other.RunIn.HasValue) RunIn = other.RunIn;
            if (other.DelayFor.HasValue) DelayFor = other.DelayFor;
            if (other.Repeat.HasValue) Repeat = other.Repeat;
            if (other.Retry.HasValue) Retry = other.Retry;
            if (other.RetryIn is not null) RetryIn = other.RetryIn;
            if (!string.IsNullOrWhiteSpace(other.TimeZone)) TimeZone = other.TimeZone;
            if (other.From.HasValue) From = other.From;
            if (other.To.HasValue) To = other.To;
            if (other.RunDuring.HasValue) RunDuring = other.RunDuring;
            if (!string.IsNullOrWhiteSpace(other.Data)) Data = other.Data;
            if (!string.IsNullOrWhiteSpace(other.Batch)) Batch = other.Batch;

            RunInThread = other.RunInThread;
            InstanceCount = other.InstanceCount;
        }

        return this;
    }

    protected string ConvertToCron(TimeSpan? span)
    {
        if (span == null) return EverySecond;

        var s = span.Value;
        int minutes = s.Minutes;
        int hours = s.Hours;
        int days = s.Days;

        if (days > 0) return $"0 0 0 */{days} * *";
        if (hours > 0) return $"0 0 */{hours} * * *";
        if (minutes > 0) return $"0 */{minutes} * * * *";
        return $"*/{s.Seconds} * * * * *";
    }
}

