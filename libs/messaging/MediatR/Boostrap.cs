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
        // Create config
        var mediatRConfig = new MediatorConfig(builder.Services);
        config?.Invoke(mediatRConfig);
        builder.Services.AddSingleton(mediatRConfig);

        // Register MediatR services
        builder.AddMiddleware<MediatorMiddleware>();
        return builder;
    }
}
