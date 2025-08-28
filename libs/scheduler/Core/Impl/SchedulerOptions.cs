namespace Sencilla.Scheduler;

/// <summary>
/// Represents the options for the scheduler.
/// </summary>
public class SchedulerOptions : ScheduledBatchOptions
{
    //IServiceCollection? Services;

    /// <summary>
    /// Gets the list of task batch options.
    /// </summary>
    /// <returns></returns>
    public Dictionary<string, ScheduledBatchOptions> ScheduleBatches { get; set; } = [];
    public Dictionary<string, ScheduledTaskOptions> ScheduleTasks { get; set; } = [];

    /// <summary>
    /// Holds the configuration options loaded from the configuration source.
    /// </summary>
    public SchedulerOptions? Configured { get; private set; }

    /// <summary>
    /// Gets the list of assemblies to scan for tasks.
    /// </summary>
    public List<Assembly>? Assemblies { get; private set; }

    /// <summary>
    /// Indicates whether to use a separate thread for each scheduler.
    /// Default is false.
    /// </summary>
    public bool ThreadPerScheduler { get; set; }

    /// <summary>
    /// Loads the assembly containing the specified type.
    /// </summary>
    /// <param name="assembly"></param>
    public void LoadTasksFromAssembly(Assembly? assembly)
    {
        if (assembly == null) return;

        Assemblies ??= new List<Assembly>();
        if (!Assemblies.Contains(assembly))
            Assemblies.Add(assembly);
    }

    /// <summary>
    /// Loads the assembly containing the specified type.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public void LoadTasksFromAssembly<T>()
    {
        LoadTasksFromAssembly(typeof(T).Assembly);
    }

    public SchedulerOptions LoadTasksFromConfig(IConfiguration configuration, string configName = "SencillaScheduler")
    {
        var section = configuration.GetSection(configName);
        LoadTasksFromConfig(section);
        return this;
    }

    public SchedulerOptions LoadTasksFromConfig(IConfigurationSection section)
    {
        if (section == null || !section.Exists() || Configured != null)
            return this;

        // Load SchedulerOptions From Configuration
        Configured = new() { Name = "" };
        section.Bind(Configured);

        return this;
    }

    // public SchedulerOptions UseServices(IServiceCollection services)
    // {
    //     Services = services;
    //     return this;
    // }

    // public SchedulerOptions CleanServices(IServiceCollection services)
    // {
    //     Services = services;
    //     return this;
    // }
    

    public ScheduledBatchOptions AddTask<T>() where T : class, IScheduledTaskHandler
    {
        return WithTask<T>();
    }

    public ScheduledBatchOptions ScheduleTask<T>(string? cron = null, string? data = null) where T : class, IScheduledTaskHandler
    {
        return WithTask<T, string>(cron, data);
    }

    public ScheduledBatchOptions ScheduleTask<T, TData>(string cron, TData? data = default) where T : class, IScheduledTaskHandler
    {
        return WithTask<T, TData>(cron, data);
    }

    public ScheduledBatchOptions ScheduleTask<T>(Action<ScheduledTaskOptions> options) where T : class, IScheduledTaskHandler
    {
        return WithTask<T>(typeof(T).Name, options);
    }


    public ScheduledBatchOptions ScheduleTask<T>(string name, Action<ScheduledTaskOptions> configure) where T : class, IScheduledTaskHandler
    {
        return WithTask<T>(name, configure);
    }

    public SchedulerOptions ScheduleBatch(string name, Action<ScheduledBatchOptions> options)
    {
        var batchOptions = new ScheduledBatchOptions { Name = name };
        options(batchOptions);
        return this;
    }
}

