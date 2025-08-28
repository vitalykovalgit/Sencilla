namespace Sencilla.Scheduler;

/// <summary>
/// Manage scheduled tasks.
/// </summary>
public interface IScheduledTasksManager : IScheduledTasksScheduler
{
    Task Init(CancellationToken cancellationToken);

    IEnumerable<IScheduledTasksScheduler> GetSchedulers();

    IScheduledTasksScheduler? GetScheduler(string name);

    IScheduledTasksScheduler GetDefaultScheduler();
}


