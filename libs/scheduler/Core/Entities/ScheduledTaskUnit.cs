namespace Sencilla.Scheduler;

/// <summary>
/// Represents a unit of time for scheduling tasks.
/// </summary>
public class SchedulerUnit
{
    public const string EverySecond = "*/1 * * * * *";
    public const string EveryMinute = "*/1 * * * *";
    public const string EveryHour = "0 * * * *";
    public const string Daily = "0 0 * * *";

    /// <summary>
    /// 
    /// </summary>
    public const long Infinite = 0;

    /// <summary>
    /// Gets the repeat count for the scheduled task.
    /// </summary>
    public const long Once = 1;

    /// <summary>
    /// Gets repeat counts 
    /// </summary>
    public const long Time = 1;
    public const long Times = 1;

    /// <summary>
    /// Gets the time unit in milliseconds.
    /// </summary>
    public const long Millisecond = 1;
    public const long Milliseconds = 1;

    public const long Second = 1000;
    public const long Seconds = 1000;

    public const long Minute = 60000;
    public const long Minutes = 60000;

    public const long Hour = 3600000;
    public const long Hours = 3600000;

    public const long Day = 86400000;
    public const long Days = 86400000;

    public const long Week = 604800000;
    public const long Weeks = 604800000;

    public const long Month = 2592000000;
    public const long Months = 2592000000;

    public const long Year = 31536000000;
    public const long Years = 31536000000;
}
