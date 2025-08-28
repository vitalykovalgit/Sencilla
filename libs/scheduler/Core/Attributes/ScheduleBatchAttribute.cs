namespace Sencilla.Scheduler;

[AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
public class ScheduleBatchAttribute : ScheduleTaskAttribute
{
    public ScheduleBatchAttribute(string? name = null, string? cron = null, string? data = null): base(name, cron, data)
    {
    }
}
