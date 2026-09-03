namespace Sencilla.Messaging;

/// <summary>
/// Distributed tracing for messages that cross a process boundary. The dispatcher stamps the
/// caller's W3C <c>traceparent</c> into the envelope metadata; a consumer starts its handler
/// activity under it, so one request's background work shows up as one trace. The source has to
/// be opted into by the host's tracer provider (<c>AddSource(MessagingActivity.SourceName)</c>) —
/// until it is, <see cref="Source"/> hands out null activities and nothing is recorded.
/// </summary>
public static class MessagingActivity
{
    public const string SourceName = "Sencilla.Messaging";

    /// <summary>Envelope metadata key carrying the W3C trace parent.</summary>
    public const string TraceParent = "traceparent";

    public static readonly ActivitySource Source = new(SourceName);
}
