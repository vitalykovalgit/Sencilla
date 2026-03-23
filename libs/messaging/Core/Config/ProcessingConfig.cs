namespace Sencilla.Messaging;

/// <summary>
/// Configures how messages are resolved and dispatched to handlers within a consumer.
/// Supports resolution by namespace (Type.GetType) and by registered type mappings.
/// </summary>
public class ProcessingConfig
{
    /// <summary>
    /// When true, resolves message types using <see cref="Type.GetType(string)"/> from the message Namespace field.
    /// This is the default resolution strategy.
    /// </summary>
    public bool ResolveByNamespace { get; private set; } = true;

    /// <summary>
    /// When true, resolves message types using a registered type mapping dictionary.
    /// </summary>
    public bool ResolveByType { get; private set; }

    /// <summary>
    /// Enables namespace-based message type resolution.
    /// The message's Namespace field is used with <see cref="Type.GetType(string)"/> to resolve the payload type.
    /// </summary>
    public ProcessingConfig ByNamespace()
    {
        ResolveByNamespace = true;
        return this;
    }

    /// <summary>
    /// Enables type-based message resolution using registered type mappings.
    /// </summary>
    public ProcessingConfig ByType()
    {
        ResolveByType = true;
        return this;
    }
}
