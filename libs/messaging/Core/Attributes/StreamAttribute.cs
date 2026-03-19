namespace Sencilla.Messaging;

/// <summary>
/// Attribute to specify stream routing for messages
/// </summary>
/// <remarks>
/// Initialize stream attribute
/// </remarks>
/// <param name="name">Stream name</param>
[AttributeUsage(AttributeTargets.Class)]
public class StreamAttribute(params string[] names) : Attribute
{
    /// <summary>
    /// Stream names
    /// </summary>
    public IEnumerable<string> Names { get; } = names;
}
