namespace Sencilla.Scheduler;

public class ScheduledTaskProviderAssembly(SchedulerOptions options): IScheduledTasksProvider
{
    public Task<ScheduledTask[]> GetTasksAsync(CancellationToken cancellationToken)
    {
        var scheduledTasks = new List<ScheduledTask>();

        // Get all assemblies to scan for scheduled tasks
        var assemblies = options.Assemblies ?? (IEnumerable<Assembly>)AppDomain.CurrentDomain.GetAssemblies();
        foreach (var assembly in assemblies)
        {
            foreach (var handler in assembly.GetTypes().Where(t => typeof(IScheduledTaskHandler).IsAssignableFrom(t) && !t.IsAbstract && t.IsClass))
            {
                // Scan for ScheduleTaskAttribute attributes
                foreach (var attribute in handler.GetCustomAttributes<ScheduleTaskAttribute>(false))
                {
                    var task = new ScheduledTask(attribute.GetOptions(handler.Name)).AddHandler(handler);
                    scheduledTasks.Add(task);

                    // iterate over all public method and get attributes
                    foreach (var method in handler.GetMethods(BindingFlags.Public | BindingFlags.Instance))
                        foreach (var attr in method.GetCustomAttributes<TaskAttribute>(false))
                        {
                            task.AddHandler(handler, method);
                        }
                }

                // Scan for ScheduleTasksAttribute attributes
                foreach (var attribute in handler.GetCustomAttributes<ScheduleTasksAttribute>(false))
                {
                    // iterate over all public method and get attributes
                    foreach (var method in handler.GetMethods(BindingFlags.Public | BindingFlags.Instance))
                        foreach (var attr in method.GetCustomAttributes<TaskAttribute>(false))
                        {
                            // TODO: Merge options from ScheduleTasksAttribute attribute
                            var task = new ScheduledTask(attr.GetOptions(method.Name)).AddHandler(handler, method);
                            scheduledTasks.Add(task);
                        }
                }

                // Scan for ScheduleTasksAttribute attributes
                foreach (var attribute in handler.GetCustomAttributes<ScheduleBatchAttribute>(false))
                {
                    // Create a new task with the options from the attribute
                    // Create only one task for batch operations
                    var task = new ScheduledTask(attribute.GetOptions(handler.Name));
                    scheduledTasks.Add(task);

                    // iterate over all public method and get attributes
                    foreach (var method in handler.GetMethods(BindingFlags.Public | BindingFlags.Instance))
                        foreach (var attr in method.GetCustomAttributes<TaskAttribute>(false))
                        {
                            task.AddHandler(handler, method);
                        }
                }
            }
        }
        return Task.FromResult(scheduledTasks.ToArray());
    }
}
