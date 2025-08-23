namespace Sencilla.Scheduler;

[AttributeUsage(AttributeTargets.Class)]
public class ScheduleCronAttribute(string name, string cronExpression) : Attribute
{
    public string Name { get; } = name;
    public string CronExpression { get; } = cronExpression;
}
