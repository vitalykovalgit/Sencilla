namespace Sencilla.Messaging;

public class MessageMiddleware
{
    private readonly ConcurrentDictionary<Type, string[]> StreamCache = [];

    protected Task SendToStream<T>(Message<T> message, ProviderConfig providerConfig, IMessageStreamProvider streamProvider)
    {
        var streams = GetStreamNames<T>(providerConfig);
        if (streams.Length == 0)
            return Task.CompletedTask;

        var tasks = streams.Select(async name =>
        {
            var config = providerConfig.Streams.GetConfig(name)
                ?? throw new InvalidOperationException($"Stream '{name}' is not defined in configuration.");

            var stream = streamProvider.GetOrCreateStream(config);
            await stream.Write(message);
        });

        return Task.WhenAll(tasks);
    }

    protected string[] GetStreamNames<T>(ProviderConfig providerConfig)
    {
        var type = typeof(T);
        if (StreamCache.TryGetValue(type, out var cached))
            return cached;

        var routeStreams = providerConfig.Routes.GetStreams<T>();

        var attrStreams = typeof(T).GetCustomAttributes(typeof(StreamAttribute), true)
            .Cast<StreamAttribute>()
            .SelectMany(x => x.Names);

        var allStreams = attrStreams.Concat(routeStreams).ToArray();
        StreamCache[type] = allStreams;
        return allStreams;
    }
}
