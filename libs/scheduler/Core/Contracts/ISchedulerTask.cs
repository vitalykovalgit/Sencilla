namespace Sencilla.Scheduler;

/// <summary>
/// Represents a scheduled task.
/// </summary>
public interface ISchedulerTask
{
    Task ExecuteAsync(CancellationToken cancellationToken);
}


