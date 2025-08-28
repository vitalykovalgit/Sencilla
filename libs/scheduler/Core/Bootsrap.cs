global using System.Reflection;
global using System.Text.Json;
global using System.Collections.Concurrent;

global using Microsoft.Extensions.Logging;
global using Microsoft.Extensions.Hosting;
global using Microsoft.Extensions.Configuration;
global using Microsoft.Extensions.DependencyInjection;
global using Microsoft.Extensions.DependencyInjection.Extensions;

global using Cronos;

global using Sencilla.Scheduler;
global using static Sencilla.Scheduler.SchedulerUnit;
using System.Diagnostics;

namespace Microsoft.Extensions.DependencyInjection;

public static class Bootstrap
{
    public static IHostApplicationBuilder AddSencillaScheduler(this IHostApplicationBuilder builder, string configName)
    {
        builder.Services.AddSencillaScheduler(builder.Configuration, configName);
        return builder;
    }

    public static IHostApplicationBuilder AddSencillaScheduler(this IHostApplicationBuilder builder, Action<SchedulerOptions>? configure = null)
    {
        builder.Services.AddSencillaScheduler(configure);
        return builder;
    }

    public static IServiceCollection AddSencillaScheduler(this IServiceCollection services, IConfiguration configuration, string configName = "SencillaScheduler")
    {
        return services.AddSencillaScheduler(options =>
        {
            options.LoadTasksFromConfig(configuration, configName);
        });
    }

    public static IServiceCollection AddSencillaScheduler(this IServiceCollection services, Assembly? assembly)
    {
        return services.AddSencillaScheduler(options =>
        {
            if (assembly != null)
                options.LoadTasksFromAssembly(assembly);
        });
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="services"></param>
    /// <param name="configure"></param>
    /// <returns></returns>
    public static IServiceCollection AddSencillaScheduler(this IServiceCollection services, Action<SchedulerOptions>? configure = null)
    {
        var existingConfig = services.FirstOrDefault(sd => sd.ServiceType == typeof(SchedulerOptions))?.ImplementationInstance as SchedulerOptions;
        var options = existingConfig ?? new SchedulerOptions { Name = string.Empty };

        // Set default configuration 
        // By default load all tasks from the caller assembly
        //options.LoadTasksFromConfig();
        options.LoadTasksFromAssembly(new StackFrame(2).GetMethod()?.DeclaringType?.Assembly);
        configure?.Invoke(options);

        // Init configuration only once
        if (existingConfig == null)
        {
            services.AddHostedService<SchedulerService>();

            services.AddSingleton(options);
            services.AddSingleton<ScheduledTasksProvider>();
            services.AddSingleton<IScheduledTasksManager, ScheduledTasksManager>();
            services.AddSingleton<IScheduledTasksProvider, ScheduledTaskProviderAssembly>();
        }

        // Register tasks from assemblies
        services.AddScheduledTasksFromAssembly(options.Assemblies);
        return services;
    }

    private static void AddScheduledTasksFromAssembly(this IServiceCollection services, params IEnumerable<Assembly>? assemblies)
    {
        if (assemblies == null) return;

        foreach (var assembly in assemblies)
        foreach (var type in assembly.GetTypes())
        {
            // Register IScheduledTaskHandler
            if (type.IsClass && !type.IsAbstract && typeof(IScheduledTaskHandler).IsAssignableFrom(type))
            {
                services.TryAddTransient(type);
                services.TryAddTransient(typeof(IScheduledTaskHandler), type);
            }

            // Register class from attributes
            if (type.GetCustomAttributes(typeof(ScheduleTasksAttribute), true).Any() ||
                type.GetCustomAttributes(typeof(ScheduleTaskAttribute), true).Any() ||
                type.GetCustomAttributes(typeof(ScheduleBatchAttribute), true).Any())
            {
                services.TryAddTransient(type);
            }
        }
    }
}
