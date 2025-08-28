namespace Sencilla.Scheduler;

/// <summary>
/// Works like descriptor for the task.
/// </summary>
public class ScheduledTask(ScheduledTaskOptions options)
{
    /// <summary>
    /// Internal identifier for the task.
    /// </summary>
    public Guid Id { get; } = Guid.NewGuid();

    /// <summary>
    /// The name of the task.
    /// </summary>
    public ScheduledTaskOptions Options { get; } = options;

    /// <summary>
    /// The handlers for the task.
    /// </summary>
    /// <value></value>
    public List<ScheduledTaskHandler>? Handlers { get; set; }

    /// <summary>
    /// The list of executions for the task.
    /// </summary>
    public List<ScheduledTaskExecution>? Executions { get; set; }

    /// <summary>
    /// Gets the time span until the next execution of the task.
    /// </summary>
    /// <param name="from"></param>
    /// <returns></returns>
    public TimeSpan GetNextOccurrence(DateTime from)
    {
        return Options.GetNextOccurrence(from);
    }

    /// <summary>
    /// The status of the task.
    /// </summary>
    /// <value></value>
    //public SchedulerTaskStatus Status { get; set; }

    public ScheduledTask SetName(string name)
    {
        Options.Name = name;
        return this;
    }

    public ScheduledTask AddHandler<T>() where T : IScheduledTaskHandler
    {
        AddHandler(typeof(T), null);
        return this;
    }

    public ScheduledTask AddHandler(Type type, MethodInfo? method = null)
    {
        Handlers ??= new List<ScheduledTaskHandler>();
        if (!Handlers.Any(h => h.HandlerType == type))
            Handlers.Add(new ScheduledTaskHandler { HandlerType = type, Method = method });
        return this;
    }

    public ScheduledTask Merge(ScheduledTask other)
    {
        Options.Merge(other.Options);

        foreach (var handler in other.Handlers ?? Enumerable.Empty<ScheduledTaskHandler>())
            AddHandler(handler.HandlerType, handler.Method);

        return this;
    }
}