namespace Sencilla.Scheduler;

[AttributeUsage(AttributeTargets.Class)]
public class ScheduleAtAttribute(string name, DateTime dateTime) : Attribute
{
    public string Name { get; } = name;
    public DateTime DateTime { get; } = dateTime;
}
