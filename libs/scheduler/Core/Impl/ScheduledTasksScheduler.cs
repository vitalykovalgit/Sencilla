namespace Sencilla.Scheduler;

/// <summary>
/// Holds and manage all scheduled tasks.
/// </summary>
/// <typeparam name="IScheduledTasksProvider"></typeparam>
public class ScheduledTasksScheduler(string name, IServiceProvider provider, IEnumerable<ScheduledTask> tasks): IScheduledTasksScheduler
{
    /// <summary>
    /// The list of scheduled tasks.
    /// </summary>
    private readonly ConcurrentDictionary<string, ScheduledTask> Tasks = new(tasks.ToDictionary(t => t.Options.Name));

    /// <summary>
    /// The task runner for executing scheduled tasks.
    /// </summary>
    /// <returns></returns>
    private readonly ScheduledTasksRunner TasksRunner = new(provider);

    /// <summary>
    /// The cancellation token source for stopping the scheduler.
    /// </summary>
    /// <returns></returns>
    private CancellationTokenSource TasksChangedToken = new();

    /// <summary>
    /// The list of tasks that are due for execution.
    /// </summary>
    private readonly List<ScheduledTask> DueTasks = [];

    public string Name { get; } = name;

    public Task<IEnumerable<ScheduledTask>> GetTasksAsync(CancellationToken cancellationToken)
    {
        return Task.FromResult<IEnumerable<ScheduledTask>>(Tasks.Values);
    }

    public Task AddTaskAsync<T>(Action<ScheduledTaskOptions> configure) where T : IScheduledTaskHandler
    {
        return AddTaskAsync<T>(typeof(T).Name, configure);
    }

    public Task AddTaskAsync<T>(string name, Action<ScheduledTaskOptions> configure) where T : IScheduledTaskHandler
    {
        var options = new ScheduledTaskOptions { Name = name };
        configure(options);
        return AddTaskAsync(options);
    }

    public Task AddTaskAsync(ScheduledTaskOptions taskOptions, CancellationToken cancellationToken = default)
    {
        Tasks[taskOptions.Name] = new ScheduledTask(taskOptions);

        // stop token waiting and re-evaluate the schedule
        var previous = Interlocked.Exchange(ref TasksChangedToken, new CancellationTokenSource());
        previous?.Cancel();
        previous?.Dispose();

        return Task.CompletedTask;
    }

    public Task RemoveTaskAsync(ScheduledTask task)
    {
        return RemoveTaskAsync(task.Options.Name);
    }

    public Task RemoveTaskAsync(string taskName)
    {
        Tasks.TryRemove(taskName, out _);

        var previous = Interlocked.Exchange(ref TasksChangedToken, new CancellationTokenSource());
        previous?.Cancel();
        previous?.Dispose();

        return Task.CompletedTask;
    }

    public async Task Schedule(CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            using var combined = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, TasksChangedToken.Token);

            // TODO: Think about uncompleted tasks!

            // Get time span for the next task execution 
            var delay = CalculateDelayAndNextTasks();

            // Create a delay task and a cancellation task
            var delayTask = Task.Delay(delay, CancellationToken.None);
            var cancellationTask = Task.Delay(Timeout.Infinite, combined.Token);

            // Wait for either the delay to complete or cancellation to be requested
            var completedTask = await Task.WhenAny(delayTask, cancellationTask);
            
            // If cancellation was requested, continue to re-evaluate
            if (completedTask == cancellationTask)
                continue;

            var task = TasksRunner.ExecuteTasks(DueTasks, cancellationToken);
            await task;
        }

        return;
    }

    private TimeSpan CalculateDelayAndNextTasks()
    {
        DueTasks.Clear();

        if (Tasks.Count == 0)
            return TimeSpan.FromMinutes(1); // TODO: Take from config

        var tasks = Tasks.Values;
        if (tasks.Count == 1)
        {
            var task = tasks.First();
            DueTasks.Add(task);
            return task.GetNextOccurrence(DateTime.UtcNow);
        }

        var time = DateTime.UtcNow;
        var minDelay = tasks.First().GetNextOccurrence(time);

        // Get all tasks with the same delay
        foreach (var task in tasks)
        {
            var next = task.GetNextOccurrence(time);
            if (minDelay > next)
            {
                minDelay = next;
                DueTasks.Clear();
            }

            if (minDelay == next)
                DueTasks.Add(task);
        }

        return minDelay;
    }
}