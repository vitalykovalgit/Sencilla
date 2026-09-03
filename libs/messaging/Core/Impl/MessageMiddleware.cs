namespace Sencilla.Messaging;

public class MessageMiddleware
{
    /// <summary>
    /// Keyed by provider config as well as type: one message type routes to different streams under
    /// different providers, and a scoped middleware instance must not rebuild this per scope.
    /// </summary>
    private static readonly ConcurrentDictionary<(ProviderConfig Provider, Type Payload), string[]> StreamCache = [];

    protected Task SendToStream<T>(Message<T> message, ProviderConfig providerConfig, IMessageStreamProvider streamProvider)
    {
        var streams = GetStreamNames<T>(providerConfig);
        if (streams.Length == 0)
            return Task.CompletedTask;

        var tasks = streams.Select(async name =>
        {
            // Only a route this provider declared itself can still miss here — a genuine
            // configuration error, so it stays loud.
            var config = providerConfig.Streams.GetConfig(name)
                ?? throw new InvalidOperationException($"Stream '{name}' is routed on this provider but not defined in its configuration.");

            var stream = streamProvider.GetOrCreateStream(config);
            await stream.Write(message);
        });

        return Task.WhenAll(tasks);
    }

    /// <summary>
    /// The streams THIS provider carries the message on. A <c>[Stream]</c> attribute is global to
    /// the type while every provider's middleware evaluates it, so a name declared by another
    /// provider is simply not this middleware's business and is dropped — it used to throw, which
    /// made the attribute unusable in any host with two transports. This provider's own fluent
    /// routes are kept as written, so a typo there still surfaces.
    /// </summary>
    protected string[] GetStreamNames<T>(ProviderConfig providerConfig)
        => StreamCache.GetOrAdd((providerConfig, typeof(T)), key =>
        {
            var (provider, type) = key;

            var attributed = type.GetCustomAttributes(typeof(StreamAttribute), true)
                .Cast<StreamAttribute>()
                .SelectMany(x => x.Names)
                .Where(name => provider.Streams.GetConfig(name) is not null);

            return attributed.Concat(provider.Routes.GetStreams(type)).Distinct().ToArray();
        });
}
