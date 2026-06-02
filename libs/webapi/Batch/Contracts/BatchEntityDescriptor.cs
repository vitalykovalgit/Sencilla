namespace Sencilla.Web.Batch;

/// <summary>
/// Everything the batch pipeline needs to act on one entity: its wire name, CLR
/// type, key type, allowed ops, and the typed invoker that calls its repositories.
/// </summary>
public sealed class BatchEntityDescriptor
{
    public required string Name { get; init; }
    public required Type EntityType { get; init; }
    public required Type KeyType { get; init; }
    public required BatchOpFlags AllowedOps { get; init; }

    /// <summary>Typed repository dispatcher for this entity (internal; always set by the registry).</summary>
    internal IBatchEntityInvoker Invoker { get; init; } = default!;

    /// <summary>Whether the entity opted into the given op.</summary>
    public bool Allows(BatchOp op) => AllowedOps.HasFlag(op.ToFlag());
}
