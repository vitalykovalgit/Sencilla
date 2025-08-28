namespace Sencilla.Scheduler;

public interface IScheduledTaskStorage
{
    //Task<IScheduledTask> GetTasksAsync(CancellationToken cancellationToken);
    Task SaveTaskAsync(ScheduledTask task, CancellationToken cancellationToken);
    Task DeleteTaskAsync(string taskId, CancellationToken cancellationToken);
}


