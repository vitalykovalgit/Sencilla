global using System;
global using System.Text.Json;
global using System.Collections.Concurrent;
global using System.Threading.Channels;

global using Sencilla.Core;
global using Sencilla.Messaging;
global using Sencilla.Messaging.InMemoryQueue;

//[assembly: AutoDiscovery]

namespace Microsoft.Extensions.DependencyInjection;

public static class Bootstrap
{
    /// <summary>
    /// Use the in-memory queue provider.
    /// </summary>
    /// <param name="builder"></param>
    /// <param name="config"></param>
    /// <returns></returns>
    public static MessagingConfig UseInMemoryQueue(this MessagingConfig builder, Action<InMemoryProviderConfig>? config)
    {
        var options = builder.AddProviderConfigOnce<InMemoryProviderConfig>(config);

        // register if not exists
        builder.AddMiddlewareOnce<InMemoryQueueMiddleware>();
        builder.AddStreamProviderOnce<InMemoryStreamProvider>();
        builder.AddHostedServiceOnce<MessageStreamsConsumer<InMemoryStreamProvider, InMemoryProviderConfig>>(options);
        
        return builder;
    }
}
