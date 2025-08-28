namespace Sencilla.Scheduler;

public class ScheduledBatchOptions : ScheduledTaskOptions
{
    /// <summary>
    /// Must be public in order to set from configuration
    /// </summary>
    public Dictionary<string, ScheduledTaskOptions> Tasks { get; set; } = [];

    // /// <summary>
    // /// Gets the list of batch tasks.
    // /// </summary>
    // /// <value></value>
    // public List<ScheduledTaskOptions> BatchTasks { get; } = [];

    // /// <summary>
    // /// Gets the list of scheduled tasks.
    // /// </summary>
    // /// <returns></returns>
    // public IEnumerable<ScheduledTaskOptions> GetTasks() => BatchTasks;

    /// <summary>
    /// Adds a scheduled task to the batch.
    /// </summary>
    /// <param name="cron"></param>
    /// <param name="data"></param>
    /// <typeparam name="T"></typeparam>
    /// <returns></returns>
    public ScheduledBatchOptions WithTask<T>(string? data = default) where T : IScheduledTaskHandler => WithTask<T, string>(null, data);
    public ScheduledBatchOptions WithTask<T>(Action<ScheduledTaskOptions> options) where T : IScheduledTaskHandler => WithTask<T>(typeof(T).Name, options);
    public ScheduledBatchOptions WithTask<T, TData>(string? cron, TData? data = default) where T : IScheduledTaskHandler => WithTask<T>(typeof(T).Name, o =>
    {
        o.Cron = cron;
        o.Data = JsonSerializer.Serialize(data);
    });
    public ScheduledBatchOptions WithTask<T>(string name, Action<ScheduledTaskOptions> configure) where T : IScheduledTaskHandler
    {
        var taskOptions = new ScheduledTaskOptions { Name = name };
        configure(taskOptions);

        Tasks[name] = taskOptions;
        return this;
    }
}

