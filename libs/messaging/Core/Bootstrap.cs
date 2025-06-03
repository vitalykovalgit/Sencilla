global using Sencilla.Core;
global using Sencilla.Messaging;

[assembly: AutoDiscovery]

namespace Microsoft.Extensions.DependencyInjection;

public static class Bootstrap
{
    public static IServiceCollection AddSencillaMessaging(this IServiceCollection builder, Action<IMessageBuilder> config)
    {
        var msgBuilder = new MessageBuilder();
        config(msgBuilder);
        return builder;
    }
}