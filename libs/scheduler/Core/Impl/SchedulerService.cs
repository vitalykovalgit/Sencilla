namespace Sencilla.Scheduler;

public class SchedulerService(IServiceProvider provider, ISchedulerTaskManager taskManager) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // Load all tasks
        var tasks = await taskManager.GetTasksAsync(stoppingToken);
    
        while (!stoppingToken.IsCancellationRequested)
        {
            // Your scheduling logic here
            Console.WriteLine("Sencilla.Scheduler running at: {0}", DateTimeOffset.Now);
            await Task.Delay(TimeSpan.FromSeconds(10), stoppingToken);
        }
    }
}
