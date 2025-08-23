namespace Sencilla.Scheduler;

public interface ISchedulerTaskProvider
{
    Task<ISchedulerTask> GetTasksAsync(CancellationToken cancellationToken);
}


