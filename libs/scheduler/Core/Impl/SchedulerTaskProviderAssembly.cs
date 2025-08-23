namespace Sencilla.Scheduler;

public class SchedulerTaskProviderAssembly : ISchedulerTaskProvider
{
    public Task<ISchedulerTask> GetTasksAsync(CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }
}