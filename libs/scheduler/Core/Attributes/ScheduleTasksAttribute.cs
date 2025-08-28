namespace Sencilla.Scheduler;

[AttributeUsage(AttributeTargets.Class)]
public class ScheduleTasksAttribute : ScheduleTaskAttribute
{
    public ScheduleTasksAttribute(string? name = null, string? cron = null, string? data = null): base(name, cron, data)
    {
    }
}

// [ScheduleTasks("Example Task")]
// public class SomeTasksHere
// {
//     [Task(Cron = "0 */5 * * * *")]
//     public Task SomeTask1(CancellationToken token)
//     {
//         return Task.CompletedTask;
//     }

//     [Task(Cron = "0 */6 * * * *")]
//     public Task SomeTask2(/*ISomeService service,*/ CancellationToken token)
//     {
//         return Task.CompletedTask;
//     }
// }

// [ScheduleBatch("Example Task", Cron = "0 */10 * * * *")]
// public class SomeBatchTasksHere
// {
//     [Task]
//     public Task BatchTask1(CancellationToken token)
//     {
//         return Task.CompletedTask;
//     }
    
//     [Task]
//     public Task BatchTask2(/*ISomeService service,*/ CancellationToken token)
//     {
//         return Task.CompletedTask;
//     }

// }