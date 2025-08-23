namespace Sencilla.Scheduler;

public class SchedulerTaskProviderConfig : ISchedulerTaskProvider
{
    public Task<ISchedulerTask> GetTasksAsync(CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }
}
