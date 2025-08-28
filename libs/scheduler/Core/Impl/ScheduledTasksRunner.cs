namespace Sencilla.Scheduler;


/// <summary>
/// Holds and manage all scheduled tasks.
/// </summary>
/// <typeparam name="IScheduledTaskProvider"></typeparam>
public class ScheduledTasksRunner(IServiceProvider provider)
{
    /// <summary>
    /// The list of tasks to run.
    /// </summary>
    //private readonly List<SchedulerTask> Tasks = [.. tasks];

    /// <summary>
    /// The currently running tasks.
    /// </summary>
    /// <returns></returns>
    //private readonly Dictionary<Guid, SchedulerTaskExecution> RunningTasks = new();

    /// <summary>
    /// The currently running instances.
    /// </summary>
    /// <returns></returns>
    //private readonly Dictionary<Guid, SchedulerTaskInstance> RunningInstances = new();


    public Task ExecuteTasks(IEnumerable<ScheduledTask> tasks, CancellationToken cancellationToken)
    {
        // Create a list to hold all task execution
        var taskExecutions = new List<Task>();

        foreach (var task in tasks)
        {
            if (cancellationToken.IsCancellationRequested)
                break;

            var executionTask = ExecuteTask(task, cancellationToken);
            taskExecutions.Add(executionTask);
        }

        return Task.WhenAll(taskExecutions);
    }

    private Task ExecuteTask(ScheduledTask schedulerTask, CancellationToken cancellationToken)
    {
        if (schedulerTask.Handlers == null || schedulerTask.Handlers.Count == 0)
            return Task.CompletedTask;

        // Create execution 
        var execution = new ScheduledTaskExecution
        {
            Task = schedulerTask,
            StartDate = DateTime.UtcNow,
            Status = ScheduledTaskStatus.Running
        };

        // Run the handler instances 
        List<Task> instanceTasks = [];
        foreach (var handlerInvoker in schedulerTask.Handlers)
        {
            // stop execution if cancellation is requested
            if (cancellationToken.IsCancellationRequested)
                break;

            for (var i = 0; i < schedulerTask.Options.InstanceCount; i++)
            {
                var instanceTask = ExecuteTaskInstance(execution, handlerInvoker, cancellationToken);
                instanceTasks.Add(instanceTask);
            }
        }

        // clean up after task execution completed
        var task = Task.WhenAll(instanceTasks).ContinueWith(t =>
        {
            // clean up after task execution completed
            execution.CompleteDate = DateTime.UtcNow;
            execution.Status = ScheduledTaskStatus.Completed;

            // TODO: Save execution info
            //RunningTasks.Remove(execution.Id);

            // TODO: Set status  

        }, cancellationToken);

        return task;
    }

    private Task ExecuteTaskInstance(ScheduledTaskExecution execution, ScheduledTaskHandler invoker, CancellationToken cancellationToken)
    {
        // Create task instance
        var instance = new ScheduledTaskInstance
        {
            SchedulerTask = execution.Task,
            CancellationToken = cancellationToken,

            StartDate = DateTime.UtcNow,
            Status = ScheduledTaskStatus.Running
        };

        // Save instances 
        execution.Instances.Add(instance);
        execution.LastExecutedInstance = instance;

        //RunningInstances.Add(instance.Id, instance);

        // Run the task and update instance info once task is completed asynchronously
        instance.Task = Task.Run(async () =>
        {
            // create a new scope for the handler
            using var scope = provider.CreateScope();
            var handler = scope.ServiceProvider.GetService(invoker.HandlerType);
            if (handler != null)
            {
                // Call interface if any 
                var taskHandler = handler as IScheduledTaskHandler;
                if (taskHandler != null)
                    await taskHandler.HandleAsync(execution.Task, cancellationToken);

                // Call method 
                if (invoker.Method != null)
                {
                    var parameters = InjectMethodParameters(scope.ServiceProvider, invoker.Method, execution.Task, cancellationToken);
                    var result = invoker.Method.Invoke(handler, parameters);
                    if (result is Task taskResult)
                        await taskResult;
                }    
            }
        },
        cancellationToken);

        // Clean up once task is completed 
        var task = instance.Task.ContinueWith(t => TaskInstanceCompleted(instance, t), cancellationToken);
        return task;
    }

    private void TaskInstanceCompleted(ScheduledTaskInstance instance, Task task)
    {
        // Update the execution info
        instance.CompleteDate = DateTime.UtcNow;
        instance.Status = task.IsFaulted ? ScheduledTaskStatus.Failed : (task.IsCanceled ? ScheduledTaskStatus.Cancelled : ScheduledTaskStatus.Completed);
        instance.Error = task.Exception?.ToString();

        // TODO: Save instance info
        //RunningInstances.Remove(instance.Id);
    }

    private object?[]? InjectMethodParameters(IServiceProvider serviceProvider, MethodInfo method, ScheduledTask task, CancellationToken cancellationToken)
    {
        var parameters = method.GetParameters();
        if (parameters.Length == 0)
            return null;

        var parameterValues = new object?[parameters.Length];
        for (int i = 0; i < parameters.Length; i++)
        {
            // Inject cancellation token if any 
            if (parameters[i].ParameterType == typeof(CancellationToken))
            {
                parameterValues[i] = cancellationToken;
                continue;
            }

            // Inject SchedulerTask if any
            if (parameters[i].ParameterType == typeof(ScheduledTask))
            {
                parameterValues[i] = task;
                continue;
            }

            var parameter = parameters[i];
            var service = serviceProvider.GetService(parameter.ParameterType);
            if (service == null && !parameter.IsOptional)
                throw new InvalidOperationException($"Cannot resolve parameter '{parameter.Name}' of type '{parameter.ParameterType}' for method '{method.Name}'.");

            parameterValues[i] = service ?? parameter.DefaultValue;
        }

        return parameterValues;
    }

    // TODO: Add disposal logic for running tasks and instances
    public void Dispose()
    {
        // Cancel all running tasks
        // foreach (var instance in RunningInstances.Values)
        // {
        //     instance.CancellationTokenSource.Cancel();
        // }

        // // Wait for all tasks to complete
        // Task.WhenAll(RunningInstances.Values.Select(i => i.Task)).ContinueWith(t =>
        // {
        //     // Dispose of all running instances
        //     foreach (var instance in RunningInstances.Values)
        //     {
        //         instance.Dispose();
        //     }

        //     // Clear the running instances dictionary
        //     RunningInstances.Clear();
        // });
    }
}