global using System.ComponentModel.DataAnnotations;
global using System.ComponentModel.DataAnnotations.Schema;
global using System.Collections.Concurrent;
global using System.Text.Json;
global using System.Text.Json.Nodes;

global using Microsoft.EntityFrameworkCore;
global using Microsoft.Extensions.DependencyInjection;
global using Microsoft.Extensions.Configuration;
global using Microsoft.Extensions.Logging;

global using Sencilla.Core;
global using Sencilla.Web;
global using Sencilla.Messaging;
global using Sencilla.Messaging.EntityFramework;
global using Sencilla.Repository.EntityFramework;

using Microsoft.Extensions.DependencyInjection.Extensions;

// Force-loaded by AddSencilla()'s scan so RepositoryRegistrator sees AppMessage/QueueMessage
// (repositories + the MessagingDbContext model).
[assembly: AutoDiscovery]

namespace Microsoft.Extensions.DependencyInjection;

public static class Bootstrap
{
    /// <summary>
    /// Adds the database as a durable messaging transport: messages become rows in [Message],
    /// claimed by workers with an optimistic RowVersion claim and acknowledged terminally.
    ///
    /// Unlike the fire-and-forget transports this one has a WRITE side that must be transactional,
    /// so enqueueing does NOT go through the dispatcher middleware (a singleton cannot reach the
    /// caller's scoped DbContext). Apps enqueue through the scoped <see cref="IMessageQueue"/>,
    /// which shares the caller's DbContext and therefore its transaction.
    /// </summary>
    public static MessagingConfig UseEntityFramework(this MessagingConfig builder, Action<EfMessagingProviderConfig>? config = null)
    {
        var providerConfig = builder.AddProviderConfigOnce(config);

        builder.Services.TryAddSingleton(providerConfig.Options);
        builder.Services.TryAddScoped<IMessageQueue, MessageQueue>();

        builder.AddStreamProviderOnce<EfMessageStreamProvider>();
        builder.AddHostedServiceOnce<MessageStreamsConsumer<EfMessageStreamProvider, EfMessagingProviderConfig>>(providerConfig);

        return builder;
    }

    /// <summary>Binds <see cref="EfMessagingOptions"/> from the "Messaging" configuration section.</summary>
    public static EfMessagingProviderConfig WithOptions(this EfMessagingProviderConfig config, IConfiguration configuration)
    {
        configuration.GetSection("Messaging").Bind(config.Options);
        return config;
    }
}
