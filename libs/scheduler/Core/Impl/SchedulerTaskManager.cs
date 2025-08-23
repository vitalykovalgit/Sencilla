
namespace Sencilla.Scheduler;

public class SchedulerTaskManager(IEnumerable<ISchedulerTaskProvider> providers) : ISchedulerTaskManager
{
    public Task<IEnumerable<ISchedulerTask>> GetTasksAsync(CancellationToken cancellationToken)
    {
        return Task.FromResult<IEnumerable<ISchedulerTask>>(Array.Empty<ISchedulerTask>());
    }
}