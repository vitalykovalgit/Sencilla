global using Sencilla.Messaging;
global using Sencilla.Messaging.Scheduler;

//global using TickerQ.DependencyInjection;
//global using TickerQ.DependencyInjection.Hosting;
//global using TickerQ.Utilities.Interfaces;

namespace Microsoft.Extensions.DependencyInjection;

public static class Bootstrap
{
    public static MessagingConfig UseScheduler(this MessagingConfig builder, Action<SchedulerProviderConfig>? config = null)
    {
        builder.AddProviderConfigOnce<SchedulerProviderConfig>(config);
        //if (builder.IsTypeNotRegistered<ITickerHost>())
        //    builder.Services.AddTickerQ();

        //builder.AddAppBuilderOnce((app, host) =>
        //{
        //    app?.UseTickerQ();
        //    host?.UseTickerQ();
        //});

        return builder;
    }
}
