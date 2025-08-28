namespace Sencilla.Scheduler;

[AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
public class ScheduleTaskAttribute : Attribute
{
    public ScheduleTaskAttribute(string? name = null, string? cron = null, string? data = null)
    {
        Name = name;
        Cron = cron;
        Data = data;
    }

    public ScheduledTaskOptions GetOptions(string name)
    {
        return new ScheduledTaskOptions
        {
            Name = Name ?? name,
            Desc = Desc,

            RunImmediately = RunImmediately,
            RunAt = RunAt == null ? null : DateTime.Parse(RunAt),
            RunIn = RunIn == 0 ? null : TimeSpan.FromMilliseconds(RunIn * Unit),
            //RunTaskAsync = RunTaskAsync,

            // How long to run
            RunDuring = RunDuring == 0 ? null : TimeSpan.FromMilliseconds(RunDuring * Unit),
            From = From == null ? null : DateTime.Parse(From),
            To = To == null ? null : DateTime.Parse(To),

            DelayFor = DelayFor == 0 ? null : TimeSpan.FromMilliseconds(DelayFor * Unit),

            Repeat = Repeat,
            Retry = Retry,
            RetryIn = RetryIn,

            //
            TimeZone = TimeZone,
            Cron = Cron,
            Every = Every == 0 ? null : TimeSpan.FromMilliseconds(Every * Unit),
            //EverySecondAt = EverySecondAt,
            //EveryMinuteAt = EveryMinuteAt,
            //EveryHourAt = EveryHourAt,
            //EveryDayAt = EveryDayAt,
            //Unit = Unit,
            //Type = Type,
            //Wait = Wait

            Data = Data,

            Batch = Batch,
            Tag = Tag
        };
    }

    /// <summary>
    /// Gets or sets the name of the scheduled task.
    /// Which can be used as a display name or for logging purposes.
    /// As a reference in config
    /// </summary>
    /// <value></value>
    public string? Name { get; set; }

    /// <summary>
    /// Gets or sets the description of the scheduled task.
    /// Which can be used to provide more context about the task.
    /// </summary>
    /// <value></value>
    public string? Desc { get; set; }

    /// <summary>
    /// Gets or sets the cron expression for the scheduled task.
    /// </summary>
    /// <value></value>
    public string? Cron { get; set; }

    /// <summary>
    /// Gets or sets the time zone for the scheduled task.
    /// </summary>
    /// <value></value>
    public string? TimeZone { get; set; }

    /// <summary>
    /// Gets or sets the interval for the scheduled task.
    /// </summary>
    /// <value></value>
    public ulong Every { get; set; } = 0;
    public string? EverySecondAt { get; set; }
    public string? EveryMinuteAt { get; set; }
    public string? EveryHourAt { get; set; }
    public string? EveryDayAt { get; set; }

    /// <summary>
    /// Gets or sets the unit of the interval for the scheduled task.
    /// </summary>
    /// <value></value>
    public ulong Unit { get; set; } = 1;

    /// <summary>
    /// Gets or sets the delay for the scheduled task.
    /// </summary>
    /// <value></value>
    public ulong DelayFor { get; set; } = 0;

    /// <summary>
    /// Gets or sets the repeat interval for the scheduled task.
    /// </summary>
    /// <value></value>
    public ulong Repeat { get; set; } = 0;

    /// <summary>
    /// Gets or sets the retry intervals for the scheduled task.
    /// </summary>
    /// <value></value>
    public ulong Retry { get; set; } = 0;

    /// <summary>
    /// Gets or sets the retry intervals for the scheduled task.
    /// </summary>
    /// <typeparam name="ulong"></typeparam>
    /// <returns></returns>
    public ulong[] RetryIn { get; set; } = Array.Empty<ulong>();

    /// <summary>
    /// Wait mode, repeat after success completion 
    /// or repeat from the start of execution 
    /// </summary>
    /// <value></value>
    public ulong Wait { get; set; } = 0;

    /// <summary>
    /// True if run task async ??? do we need it 
    /// </summary>
    /// <value></value>
    //public bool RunTaskAsync { get; set; }

    /// <summary>
    /// How many instance of handler to run 
    /// </summary>
    /// <value></value>
    public ulong InstanceCount { get; set; } = 1;

    /// <summary>
    /// Gets or sets the data for the scheduled task.
    /// </summary>
    /// <value></value>
    public string? Data { get; set; }

    /// <summary>
    /// Date time when the task should start.
    /// </summary>
    /// <value></value>
    public string? From { get; set; }

    /// <summary>
    /// Gets or sets the end date and time for the scheduled task.
    /// </summary>
    /// <value></value>
    public string? To { get; set; }

    /// <summary>
    /// Like run during 6 month/day/hour from now
    /// </summary>
    /// <value></value>
    public ulong RunDuring { get; set; } = 0;

    /// <summary>
    /// Gets or sets a value indicating whether the task should run in a separate thread.
    /// </summary>
    /// <value></value>
    public bool RunInThread { get; set; }

    /// <summary>
    /// Gets or sets the run behavior for the scheduled task.
    /// </summary>
    public bool RunImmediately { get; set; } = true;

    /// <summary>
    /// Gets or sets the time at which the scheduled task should run.
    /// </summary>
    /// <value></value>
    public string? RunAt { get; set; }

    /// <summary>
    /// Gets or sets the delay for the scheduled task.
    /// </summary>
    /// <value></value>
    public ulong RunIn { get; set; } = 0;


    /// <summary>
    /// Gets or sets the tag for the scheduled task.
    /// </summary>
    public string? Tag { get; set; }

    /// <summary>
    /// Gets or sets the batch for the scheduled task.
    /// </summary>
    public string? Batch { get; set; }

    /// <summary>
    /// Gets or sets the scheduler for the scheduled task.
    /// </summary>
    public string? Scheduler { get; set; }
}

// [ScheduleTask()]
// [ScheduleTask(Every = 5*Seconds)]
// [ScheduleTask("SimpleTask")]
// [ScheduleTask("SimpleTask", EveryMinute)]
// [ScheduleTask("MyTask1", WithCron = "0 * * * *", Repeat = 10 * Times)]
// [ScheduleTask("MyTask1", WithCron = "0 * * * *", Repeat = Once)]
// [ScheduleTask("MyTask1", Cron = "0 * * * *", Repeat = Infinite)]
// [ScheduleTask("MyTask1", RunAt = "2024-12-31T23:59:59")]
// [ScheduleTask("MyTask2", Every = 1*Minute)]
// [ScheduleTask("MyTask2", Every = 1*Hour, DelayFor = 60*Minute, RetryIn = [10*Seconds, 20*Seconds, 30*Seconds])]
// public class SampleTask : IScheduledTaskHandler
// {
//     public Task HandleAsync(ScheduledTask task, CancellationToken cancellationToken)
//     {
//         throw new NotImplementedException();
//     }
// }

// [ScheduleTask("SampleTask2", WithCron = "0 * * * *", Batch = "batch")]
// public class SampleTask2 : ISchedulerTaskHandler
// {
//     public Task HandleAsync(SchedulerTask task, CancellationToken cancellationToken) => Task.CompletedTask;
// }

// [ScheduleTask("SampleTask3", WithCron = "0 * * * *", Batch = "batch")]
// public class SampleTask3: ISchedulerTaskHandler
// {
//     public Task HandleAsync(SchedulerTask task, CancellationToken cancellationToken) => Task.CompletedTask;
// }