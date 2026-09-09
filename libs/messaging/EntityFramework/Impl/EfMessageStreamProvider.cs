namespace Sencilla.Messaging.EntityFramework;

/// <summary>
/// One <see cref="EfMessageStream"/> per stream name. Singleton like every stream provider; the
/// streams open a scope per database operation rather than holding a DbContext.
/// </summary>
[DisableInjection]
public class EfMessageStreamProvider(
    IServiceScopeFactory scopeFactory,
    EfMessagingOptions options,
    ILoggerFactory loggerFactory) : IMessageStreamProvider
{
    private readonly ConcurrentDictionary<string, EfMessageStream> Streams = [];

    public IMessageStream GetOrCreateStream(StreamConfig streamConfig)
    {
        var name = streamConfig?.Name
            ?? throw new ArgumentNullException(nameof(streamConfig), "Stream name cannot be null.");

        return Streams.GetOrAdd(name, _ =>
            new EfMessageStream(scopeFactory, streamConfig, options, loggerFactory.CreateLogger<EfMessageStream>()));
    }

    public IMessageStream? GetStream(StreamConfig streamConfig)
    {
        if (streamConfig?.Name is null)
            return null;

        Streams.TryGetValue(streamConfig.Name, out var stream);
        return stream;
    }

    /// <summary>Wake the reader on a stream — the hook a doorbell transport calls.</summary>
    public void Notify(string stream)
    {
        if (Streams.TryGetValue(stream, out var found))
            found.Notify();
    }
}
