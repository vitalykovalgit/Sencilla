namespace Sencilla.Scheduler;

public class ScheduledTasksProvider(SchedulerOptions options, IEnumerable<IScheduledTasksProvider> taskProviders)
{ 
    /// <summary>   
    /// Configuration has the most high precedence
    /// </summary>
    /// <returns></returns>
    public async Task<Dictionary<string, ScheduledTask>> GetTasks(CancellationToken cancellationToken)
    {
        // Load tasks from different sources
        List<ScheduledTask> domainTasks = [];
        foreach (var provider in taskProviders)
        {
            var tasks = await provider.GetTasksAsync(cancellationToken);
            domainTasks.AddRange(tasks);
        }

        // Load from builder and configuration 
        var builderTasks = LoadFromBuilder();
        var configuredTasks = LoadFromConfiguration();

        // Merge all tasks
        // The configured tasks has the most high precedence
        // and rewrite attribute and builder
        Dictionary<string, ScheduledTask> allTasks = [];
        MergeTasks(allTasks, domainTasks);
        MergeTasks(allTasks, builderTasks);
        MergeTasks(allTasks, configuredTasks);

        // Finalize all tasks
        return allTasks;
    }

    protected void MergeTasks(Dictionary<string, ScheduledTask> allTasks, IEnumerable<ScheduledTask> newTasks)
    {
        // Merge tasks by name
        foreach (var task in newTasks)
        {
            if (!allTasks.ContainsKey(task.Options.Name))
                allTasks[task.Options.Name] = task;
            else
                allTasks[task.Options.Name].Merge(task);
        }
    }

    protected IEnumerable<ScheduledTask> LoadFromBuilder()
    {
        return options.Tasks.Select(kvp => new ScheduledTask(kvp.Value)).ToList();
    }

    protected IEnumerable<ScheduledTask> LoadFromConfiguration()
    {
        // Load scheduled tasks from configuration
        // TODO: Load batches from configuration
        return options.Configured?
                      .ScheduleTasks
                      .Select(kvp => new ScheduledTask(kvp.Value).SetName(kvp.Key))
                      ?? Enumerable.Empty<ScheduledTask>();
    }
}