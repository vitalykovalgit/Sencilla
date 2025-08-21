global using System.Collections.Concurrent;
global using System.Collections.Generic;

global using Microsoft.Extensions.DependencyInjection;
global using Microsoft.Extensions.Logging;
global using Microsoft.Extensions.Hosting;

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
        var options = new MessagingConfig(services);
        config(options);
        options.Cleanup();

        // Register the messaging configuration
        services.AddSingleton(options);

        return services;
    }
}