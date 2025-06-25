namespace Sencilla.Messaging.InMemoryQueue;

public class ConsumerService(MessagingConfig config, IServiceProvider serviceProvider, ILogger<ConsumerService> logger) : BackgroundService
{
    List<ProviderConsumerService> ProviderServices = [];

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("ConsumerService::ExecuteAsync is called at utctime: {time}", DateTimeOffset.UtcNow);

        List<Task> tasks = [];

        // Start consumer for every queue provider
        config.Providers.Values.ForEach(provider =>
        {
            var service = new ProviderConsumerService(
                serviceProvider.GetService<ILogger<ProviderConsumerService>>(),
                serviceProvider.GetService<IMessageBroker>(),
                provider);

            ProviderServices.Add(service);

            tasks.Add(Task.Run(async () =>
            {
                await service.Execute(stoppingToken);
            },
            stoppingToken));
        });

        await Task.WhenAll(tasks);
    }
}

