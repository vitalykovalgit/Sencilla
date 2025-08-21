namespace Sencilla.Messaging;

public class MessageMiddleware
{
    /// <summary>
    /// Cache for storing stream names associated with message types.
    /// </summary>
    /// <returns></returns>
    private readonly ConcurrentDictionary<Type, IEnumerable<string>> _streamCache = new();

    /// <summary>
    /// 
    /// </summary>
    /// <param name="message"></param>
    /// <param name="providerConfig"></param>
    /// <param name="streamProvider"></param>
    /// <typeparam name="T"></typeparam>
    /// <returns></returns>
    protected Task SendToStream<T>(Message<T> message, ProviderConfig providerConfig, IMessageStreamProvider streamProvider)
    {
        // 1. Get stream name for the message 
        var streams = GetStreamNames<T>(providerConfig);
        if (streams is null || !streams.Any())
            return Task.CompletedTask;

        var tasks = streams.Select(async name =>
        {
            // Get stream
            var config = providerConfig.Streams.GetConfig(name) ?? throw new InvalidOperationException($"Stream '{name}' is not defined in configuration.");
            var stream = streamProvider.GetStream(config);
            if (stream is not null)
                await stream.Write(message);
        });

        return Task.WhenAll(tasks);
    }

    /// <summary>
    /// 
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <returns></returns>
    protected IEnumerable<string>? GetStreamNames<T>(ProviderConfig providerConfig)
    {
        var type = typeof(T);
        if (_streamCache.ContainsKey(type))
            return _streamCache[type];

        // Get streams from route config
        var routeStreams = providerConfig.Routes.GetStreams<T>();

        // Get streams from attribute 
        var attrStreams = typeof(T).GetCustomAttributes(typeof(StreamAttribute), true)
                               .Cast<StreamAttribute>()
                               .SelectMany(x => x.Names) ?? Array.Empty<string>();

        var allStreams = attrStreams.Concat(routeStreams).ToArray();
        _streamCache[type] = allStreams;
        return allStreams;
    }
}
