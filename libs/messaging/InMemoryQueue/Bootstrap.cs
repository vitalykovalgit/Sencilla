global using System;
global using System.Text.Json;
global using System.Collections.Concurrent;
global using System.Threading.Channels;

global using Microsoft.Extensions.Hosting;
global using Microsoft.Extensions.Logging;
global using Microsoft.Extensions.DependencyInjection;

global using Sencilla.Core;
global using Sencilla.Messaging;
global using Sencilla.Messaging.InMemoryQueue;

[assembly: AutoDiscovery]

namespace Microsoft.Extensions.DependencyInjection;

public static class Bootstrap
{
    public static MessagingConfig UseInMemoryQueue(this MessagingConfig builder, Action<InMemoryQueueConfig>? config)
    {
        // Create config
        var inMemoryQueueConfig = new InMemoryQueueConfig(builder);
        config?.Invoke(inMemoryQueueConfig);
        builder.Services.AddSingleton(inMemoryQueueConfig);

        // Add the in-memory queue middleware
        builder.AddMiddleware<InMemoryQueueMiddleware>();

        return builder;
    }
}
