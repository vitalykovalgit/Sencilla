global using Microsoft.Extensions.Hosting;
global using Sencilla.Scheduler;

namespace Microsoft.Extensions.DependencyInjection;

public static class Bootstrap
{
    public static IServiceCollection AddSencillaScheduler(this IServiceCollection services, Action<SchedulerOptions> configure)
    {
        //services.AddSingleton<ISchedulerProvider, SchedulerProvider>();
        //services.AddSingleton<ISchedulerService, SchedulerService>();

        services.Configure(configure);
        services.AddHostedService<SchedulerService>();

        return services;
    }
}
