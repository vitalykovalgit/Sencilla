global using System.Collections.Concurrent;
global using System.Collections.Generic;

global using Microsoft.Extensions.DependencyInjection;
global using Microsoft.Extensions.Logging;
global using Microsoft.Extensions.Hosting;

global using Microsoft.AspNetCore.Builder;

global using Sencilla.Core;
global using Sencilla.Messaging;

[assembly: AutoDiscovery]

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
        }

        return services;
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