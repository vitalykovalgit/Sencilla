global using System.Collections.Concurrent;

global using Microsoft.Extensions.DependencyInjection;

global using Sencilla.Core;
global using Sencilla.Messaging;

[assembly: AutoDiscovery]

namespace Microsoft.Extensions.DependencyInjection;

public static class Bootstrap
{
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