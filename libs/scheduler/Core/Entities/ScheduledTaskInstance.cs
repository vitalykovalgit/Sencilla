namespace Sencilla.Scheduler;

public class ScheduledTaskInstance
{
    public Guid Id { get; } = Guid.NewGuid();

    public Task? Task { get; set; }

    public required ScheduledTask SchedulerTask { get; init; }

    public required CancellationToken CancellationToken { get; init; }

    public DateTime StartDate { get; set; } = DateTime.UtcNow;

    public DateTime? CompleteDate { get; set; }

    public ScheduledTaskStatus Status { get; set; } = ScheduledTaskStatus.Running;

    public string? Error { get; set; }
}
