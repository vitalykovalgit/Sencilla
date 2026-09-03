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
    /// The write side is transactional: <see cref="EfQueueMiddleware"/> is scoped and writes through
    /// the caller's <see cref="IMessageQueue"/>, so a message dispatched inside a transaction commits
    /// with it. Route messages here with <c>[Stream("name")]</c> on the payload type or
    /// <c>ef.AddRoutes(r => r.SendToStream("name", typeof(T)))</c>; both must name a queue declared
    /// in <c>ef.AddStreams(...)</c>. The message then continues down the pipeline: an in-process
    /// Mediator in the same host skips <c>[Stream]</c> types unless it opts in with
    /// <c>HandleDurable()</c>, so registration order does not matter.
    /// </summary>
    public static MessagingConfig UseEntityFramework(this MessagingConfig builder, Action<EfMessagingProviderConfig>? config = null)
    {
        var providerConfig = builder.AddProviderConfigOnce(config);

        builder.Services.TryAddSingleton(providerConfig.Options);
        builder.Services.TryAddScoped<IMessageQueue, MessageQueue>();
        builder.AddMiddlewareOnce<EfQueueMiddleware>(ServiceLifetime.Scoped);

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
