namespace Sencilla.Scheduler;

/// <summary>
/// 
/// </summary>
public interface ISchedulerTaskManager
{
    /// <summary>
    /// 
    /// </summary>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    Task<IEnumerable<ISchedulerTask>> GetTasksAsync(CancellationToken cancellationToken);
}


