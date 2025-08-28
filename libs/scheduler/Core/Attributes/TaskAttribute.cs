namespace Sencilla.Scheduler;

/// <summary>
/// Attribute used to mark methods as scheduled tasks.
/// This attribute inherits from ScheduleTaskAttribute and provides a convenient way
/// to define scheduled tasks with cron expressions and optional metadata.
/// </summary>
[AttributeUsage(AttributeTargets.Method, AllowMultiple = true)]
public class TaskAttribute : ScheduleTaskAttribute
{
    /// <summary>
    /// Initializes a new instance of the TaskAttribute class.
    /// </summary>
    /// <param name="name">Optional name for the task. If not provided, the method name will be used.</param>
    /// <param name="cron">Optional cron expression that defines when the task should run.</param>
    /// <param name="data">Optional data that can be passed to the task when it executes.</param>
    public TaskAttribute(string? name = null, string? cron = null, string? data = null) : base(name, cron, data)
    {
    }
}
