global using System;
global using System.Collections.Concurrent;
global using System.Text;
global using System.Text.Json;
global using System.Threading.Channels;

global using Microsoft.Extensions.DependencyInjection;
global using Microsoft.Extensions.Hosting;
global using Microsoft.Extensions.Logging;
global using Microsoft.Extensions.Diagnostics.HealthChecks;

global using RabbitMQ.Client;
global using RabbitMQ.Client.Events;

global using Sencilla.Core;
global using Sencilla.Messaging;
global using Sencilla.Messaging.RabbitMQ;

namespace Microsoft.Extensions.DependencyInjection;

public static class Bootstrap
{
    /// <summary>
    /// Adds RabbitMQ as a messaging provider.
    /// </summary>
    public static MessagingConfig UseRabbitMQ(this MessagingConfig builder, Action<RabbitMQProviderConfig>? config)
    {
        var options = builder.AddProviderConfigOnce<RabbitMQProviderConfig>(config);

        // Register RabbitMQ-specific services
        if (!builder.Services.Any(sd => sd.ServiceType == typeof(IRabbitMQConnectionFactory)))
        {
            builder.Services.AddSingleton<IRabbitMQConnectionFactory>(sp =>
                new RabbitMQConnectionFactory(options.Options,
                    sp.GetRequiredService<ILogger<RabbitMQConnectionFactory>>()));

            builder.Services.AddSingleton<IRabbitMQTopologyManager>(sp =>
                new RabbitMQTopologyManager(
                    sp.GetRequiredService<IRabbitMQConnectionFactory>(),
                    options.Options,
                    sp.GetRequiredService<ILogger<RabbitMQTopologyManager>>()));

            builder.Services.AddHealthChecks()
                .AddCheck<RabbitMQHealthCheck>("rabbitmq", tags: ["messaging", "rabbitmq"]);
        }

        builder.AddMiddlewareOnce<RabbitMQMiddleware>();
        builder.AddStreamProviderOnce<RabbitMQStreamProvider>();
        builder.AddHostedServiceOnce<MessageStreamsConsumer<RabbitMQStreamProvider, RabbitMQProviderConfig>>(options);

        return builder;
    }
}
