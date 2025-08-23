namespace Sencilla.Scheduler;

[AttributeUsage(AttributeTargets.Class)]
public class ScheduleInAttribute(string name, TimeSpan interval) : Attribute
{
    public string Name { get; } = name;
    public TimeSpan Interval { get; } = interval;
}
