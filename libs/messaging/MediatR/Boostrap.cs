global using Microsoft.Extensions.DependencyInjection;

global using Sencilla.Core;
global using Sencilla.Messaging;
global using Sencilla.Messaging.Mediator;

namespace Microsoft.Extensions.DependencyInjection;

public static class Boostrap
{
    /// <summary>
    /// Bootstrap method to add MediatR support to the messaging system.
    /// This method allows you to configure MediatR settings through a provided action delegate.
    /// </summary>
    /// <param name="builder"></param>
    /// <param name="config"></param>
    /// <returns></returns>
    public static MessagingConfig UseMediator(this MessagingConfig builder, Action<MediatorConfig>? config = null)
    {
        // Register Mediator services
        builder.AddProviderConfigOnce<MediatorConfig>(config);
        builder.AddMiddlewareOnce<MediatorMiddleware>();
        return builder;
    }
}
