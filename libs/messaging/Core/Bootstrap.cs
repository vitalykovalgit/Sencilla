global using System.Collections.Concurrent;
global using System.Collections.Generic;
global using System.Reflection;
global using System.Text.Json;

global using Microsoft.Extensions.DependencyInjection;
global using Microsoft.Extensions.Logging;
global using Microsoft.Extensions.Hosting;

global using Microsoft.AspNetCore.Builder;

global using Sencilla.Core;
global using Sencilla.Messaging;

namespace Microsoft.Extensions.DependencyInjection;

public static class Bootstrap
{
    /// <summary>
    /// Adds Sencilla messaging services to the specified <see cref="IServiceCollection"/>.
    /// </summary>
    /// <param name="services"></param>
    /// <param name="config">The configuration action to customize the messaging options.</param>
    /// <returns> The provided service collection.</returns>
    public static IServiceCollection AddSencillaMessaging(this IServiceCollection services, Action<MessagingConfig> config)
    {
        var existingConfig = services.FirstOrDefault(sd => sd.ServiceType == typeof(MessagingConfig))?.ImplementationInstance as MessagingConfig;
        var options = existingConfig ?? new MessagingConfig();

        options.Init(services);
        config(options);
        options.Cleanup();

        // Register the messaging configuration
        if (existingConfig == null)
        {
            services.AddSingleton(options);
            services.AddSingleton<IMessageDispatcher, MessageDispatcher>();
            services.AddSingleton<IMessageHandlerExecutor, MessageHandlerExecutor>();
        }

        return services;
    }

    /// <summary>
    /// Adds Sencilla messaging services using <see cref="IMessagingConfig"/> implementations
    /// discovered from the specified assemblies.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="assemblies">Assemblies to scan for <see cref="IMessagingConfig"/> implementations.</param>
    public static IServiceCollection AddSencillaMessaging(this IServiceCollection services, params Assembly[] assemblies)
    {
        return services.AddSencillaMessaging(options =>
        {
            options.ApplyConfigurationsFromAssemblies(assemblies);
        });
    }

    public static void UseSencillaMessaging(this IApplicationBuilder app)
    {
        var messagingConfig = app.ApplicationServices.GetRequiredService<MessagingConfig>();
        messagingConfig.BuildApp(app);
    }

    public static void UseSencillaMessaging(this IHost app)
    {
        var messagingConfig = app.Services.GetRequiredService<MessagingConfig>();
        messagingConfig.BuildApp(app);
    }
}