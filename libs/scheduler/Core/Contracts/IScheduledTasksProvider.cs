namespace Sencilla.Scheduler;

/// <summary>
/// Provides scheduled tasks from different sources
/// Like 
/// - from code using attribute 
/// - from databases 
/// - from files  
/// - from storages, etc 
/// </summary>
public interface IScheduledTasksProvider
{
    /// <summary>
    /// Returns all scheduled tasks.
    /// </summary>
    /// <param name="cancellationToken"></param>
    /// <returns>  </returns>
    Task<ScheduledTask[]> GetTasksAsync(CancellationToken cancellationToken);
}


