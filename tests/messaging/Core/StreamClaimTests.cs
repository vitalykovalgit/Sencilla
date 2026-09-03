namespace Sencilla.Messaging.Tests;

/// <summary>
/// [Stream] is global to a type while every provider's middleware evaluates it, so a provider must
/// claim only the streams it declares — and stay loud about its own misrouted fluent routes.
/// </summary>
public class StreamClaimTests
{
    private class Probe : MessageMiddleware
    {
        public string[] Streams<T>(ProviderConfig config) => GetStreamNames<T>(config);
        public Task Send<T>(Message<T> message, ProviderConfig config, IMessageStreamProvider provider) => SendToStream(message, config, provider);
    }

    private class RecordingStream(string name) : IMessageStream
    {
        public string Name => name;
        public List<Message> Written { get; } = [];
        public Task<string?> Read(CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task<Message<T>?> Read<T>(CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task Write<T>(Message<T>? message, CancellationToken cancellationToken = default)
        {
            Written.Add(message!);
            return Task.CompletedTask;
        }
    }

    private class RecordingProvider : IMessageStreamProvider
    {
        public Dictionary<string, RecordingStream> Streams { get; } = [];
        public IMessageStream? GetStream(StreamConfig config) => Streams.GetValueOrDefault(config.Name!);
        public IMessageStream GetOrCreateStream(StreamConfig config)
            => Streams.TryGetValue(config.Name!, out var s) ? s : Streams[config.Name!] = new RecordingStream(config.Name!);
    }

    [Stream("elsewhere")] private class QueuedElsewhere;
    [Stream("mine")] private class QueuedHere;
    private class RoutedByFluent;

    private static ProviderConfig Provider()
    {
        var config = new ProviderConfig();
        config.Streams.AddQueue("mine");
        return config;
    }

    [Fact]
    public async Task AttributeNamingAnotherProvidersStream_IsNotThisProvidersBusiness()
    {
        var provider = new RecordingProvider();
        var config = Provider();

        Assert.Empty(new Probe().Streams<QueuedElsewhere>(config));
        await new Probe().Send(new Message<QueuedElsewhere> { Payload = new() }, config, provider); // no throw

        Assert.Empty(provider.Streams);
    }

    [Fact]
    public async Task AttributeNamingADeclaredStream_IsClaimed()
    {
        var provider = new RecordingProvider();
        var config = Provider();

        Assert.Equal(["mine"], new Probe().Streams<QueuedHere>(config));
        await new Probe().Send(new Message<QueuedHere> { Payload = new() }, config, provider);

        Assert.Single(provider.Streams["mine"].Written);
    }

    [Fact]
    public async Task FluentRouteToAnUndeclaredStream_StillFailsLoudly()
    {
        var config = Provider();
        config.Routes.SendToStream("typo", typeof(RoutedByFluent));

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => new Probe().Send(new Message<RoutedByFluent> { Payload = new() }, config, new RecordingProvider()));
    }
}
