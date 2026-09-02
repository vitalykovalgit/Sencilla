namespace Sencilla.Messaging;

/// <summary>
/// The one serializer configuration for message envelopes and payloads. Web defaults mean stored
/// JSON is camelCase like every other payload in the platform, and reads are case-insensitive — so
/// a consumer still binds envelopes written by an older transport that used PascalCase.
/// </summary>
public static class MessageJson
{
    public static readonly JsonSerializerOptions Options = new(JsonSerializerDefaults.Web);
}
