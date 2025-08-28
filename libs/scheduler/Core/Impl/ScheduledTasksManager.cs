namespace Sencilla.Scheduler;

/// <summary>
/// Holds and manage all scheduled tasks with their schedulers.
/// </summary>
public class ScheduledTasksManager(SchedulerOptions options, IServiceProvider provider, ScheduledTasksProvider tasksProvider) : IScheduledTasksManager
{
    private readonly List<IScheduledTasksScheduler> schedulers = new();
    private ScheduledTasksScheduler? DefaultScheduler;

    public async Task Init(CancellationToken cancellationToken = default)
    {
        // Check if already initialized
        if (DefaultScheduler != null)
            return;

        // Load all tasks 
        var tasks = await tasksProvider.GetTasks(cancellationToken);

        DefaultScheduler = new ScheduledTasksScheduler("_sencilla_default_scheduler_", provider, tasks.Values);
        schedulers.Add(DefaultScheduler);

        // Analyze tasks and create schedulers
        // foreach (var task in tasks.Values)
        // {
        //     var scheduler = new ScheduledTasksScheduler(task.Options.Name, provider, new[] { task });
        //     schedulers.Add(scheduler);
        // }
    }

    public string Name => string.Empty;

    public Task AddTaskAsync(ScheduledTaskOptions taskOptions, CancellationToken cancellationToken = default)
    {
        return DefaultScheduler?.AddTaskAsync(taskOptions, cancellationToken) ?? Task.CompletedTask;
    }

    public Task AddTaskAsync<T>(Action<ScheduledTaskOptions> configure) where T : IScheduledTaskHandler
    {
        return DefaultScheduler?.AddTaskAsync<T>(configure) ?? Task.CompletedTask;
    }

    public Task AddTaskAsync<T>(string name, Action<ScheduledTaskOptions> configure) where T : IScheduledTaskHandler
    {
        return DefaultScheduler?.AddTaskAsync<T>(name, configure) ?? Task.CompletedTask;
    }

    public IScheduledTasksScheduler GetDefaultScheduler()
    {
        return DefaultScheduler ?? throw new InvalidOperationException("The default scheduler is not initialized. Call Init() first.");
    }

    public IScheduledTasksScheduler? GetScheduler(string name)
    {
        return schedulers.FirstOrDefault(s => s.Name == name);
    }

    public IEnumerable<IScheduledTasksScheduler> GetSchedulers()
    {
        return schedulers;
    }

    public Task<IEnumerable<ScheduledTask>> GetTasksAsync(CancellationToken cancellationToken)
    {
        return DefaultScheduler?.GetTasksAsync(cancellationToken) ?? Task.FromResult(Enumerable.Empty<ScheduledTask>());
    }

    public Task RemoveTaskAsync(string taskName)
    {
        return DefaultScheduler?.RemoveTaskAsync(taskName) ?? Task.CompletedTask;
    }

    public Task RemoveTaskAsync(ScheduledTask task)
    {
        return DefaultScheduler?.RemoveTaskAsync(task) ?? Task.CompletedTask;
    }

    public Task Schedule(CancellationToken cancellationToken)
    {
        // Schedule all task
        List<Task> tasks = [];
        if (options.ThreadPerScheduler)
        {
            foreach (var scheduler in schedulers)
                tasks.Add(Task.Run(async () => await scheduler.Schedule(cancellationToken), cancellationToken));
        }
        else
        {
            tasks.AddRange(schedulers.Select(s => s.Schedule(cancellationToken)));
        }

        return Task.WhenAll(tasks);
    }
}