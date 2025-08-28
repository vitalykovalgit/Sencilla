namespace Sencilla.Scheduler;

/// <summary>
/// Represents a scheduler for managing scheduled tasks.
/// </summary>
public interface IScheduledTasksScheduler
{
    /// <summary>
    /// The name of the scheduler.
    /// </summary>
    /// <value></value>
    string Name { get; }

    /// <summary>
    /// Retrieves all scheduled tasks.
    /// </summary>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    Task<IEnumerable<ScheduledTask>> GetTasksAsync(CancellationToken cancellationToken);

    /// <summary>
    /// Adds a new scheduled task.
    /// </summary>
    /// <param name="taskOptions"></param>
    Task AddTaskAsync(ScheduledTaskOptions taskOptions, CancellationToken cancellationToken = default);

    /// <summary>
    /// Adds a new scheduled task.
    /// </summary>
    /// <param name="configure"></param>
    /// <typeparam name="T"></typeparam>
    Task AddTaskAsync<T>(Action<ScheduledTaskOptions> configure) where T : IScheduledTaskHandler;

    /// <summary>
    /// 
    /// </summary>
    /// <param name="name"></param>
    /// <param name="configure"></param>
    /// <typeparam name="T"></typeparam>
    Task AddTaskAsync<T>(string name, Action<ScheduledTaskOptions> configure) where T : IScheduledTaskHandler;

    /// <summary>
    /// Removes a scheduled task.
    /// </summary>
    /// <param name="taskName"></param>
    /// <returns></returns>
    Task RemoveTaskAsync(string taskName);

    /// <summary>
    /// Removes a scheduled task.
    /// </summary>
    /// <param name="task"></param>
    /// <returns></returns>
    Task RemoveTaskAsync(ScheduledTask task);

    /// <summary>
    /// Schedules the execution of tasks.
    /// </summary>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    Task Schedule(CancellationToken cancellationToken);
}


