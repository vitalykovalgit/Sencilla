namespace Sencilla.Scheduler;

public class SchedulerService(ILogger<SchedulerService> logger, IScheduledTasksManager tasksManager) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // Finalize all tasks
        logger.LogInformation("SchedulerService::ExecuteAsync is called");
        await tasksManager.Init(stoppingToken);
        await tasksManager.Schedule(stoppingToken);
    }
}
