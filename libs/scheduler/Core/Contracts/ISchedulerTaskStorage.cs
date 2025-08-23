namespace Sencilla.Scheduler;

public interface ISchedulerTaskStorage
{
    //Task<ISchedulerTask> GetTasksAsync(CancellationToken cancellationToken);
    Task SaveTaskAsync(ISchedulerTask task, CancellationToken cancellationToken);
    Task DeleteTaskAsync(string taskId, CancellationToken cancellationToken);
}


