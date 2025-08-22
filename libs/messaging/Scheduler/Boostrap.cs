global using Sencilla.Messaging;
global using Sencilla.Messaging.Scheduler;

namespace Microsoft.Extensions.DependencyInjection;

public static class Bootstrap
{
    public static MessagingConfig UseScheduler(this MessagingConfig builder, Action<SchedulerProviderConfig>? config = null)
    {
        builder.AddProviderConfigOnce<SchedulerProviderConfig>(config);
        builder.AddAppBuilderOnce(app =>
        {
            Console.WriteLine("Called 1");
            //var logger = app.ApplicationServices.GetRequiredService<ILogger<SchedulerMiddleware>>();
            //app.UseMiddleware<SchedulerMiddleware>(logger);
        });

        return builder;
    }
}
