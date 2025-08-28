namespace Sencilla.Scheduler;

/// <summary>
/// Represents a scheduled task handler.
/// </summary>
public interface IScheduledTaskHandler
{
    /// <summary>
    /// Handles the scheduled task.
    /// </summary>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    Task HandleAsync(ScheduledTask task, CancellationToken cancellationToken);
}


// TODO: Add IScheduledTaskExceptionHandler