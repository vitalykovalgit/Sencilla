global using System;
global using System.Text.Json;
global using System.Collections.Concurrent;
global using System.Threading.Channels;
global using Microsoft.AspNetCore.SignalR;

global using Sencilla.Core;
global using Sencilla.Messaging;
global using Sencilla.Messaging.SignalR;

namespace Microsoft.Extensions.DependencyInjection;

public static class Bootstrap
{
    /// <summary>
    /// Use the SignalR messaging provider.
    /// </summary>
    /// <param name="builder"></param>
    /// <param name="config"></param>
    /// <returns></returns>
    public static MessagingConfig UseSignalR(this MessagingConfig builder, Action<SignalRProviderConfig>? config)
    {
        var options = builder.AddProviderConfigOnce<SignalRProviderConfig>(config);

        // register if not exists
        builder.AddMiddlewareOnce<SignalRQueueMiddleware>();
        builder.AddStreamProviderOnce<SignalRStreamProvider>();
        builder.AddHostedServiceOnce<MessageStreamsConsumer<SignalRStreamProvider, SignalRProviderConfig>>(options);

        // Add SignalR services
        builder.Services.AddSignalR();
        
        return builder;
    }
}
