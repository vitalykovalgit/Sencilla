namespace Sencilla.Web.Batch;

/// <summary>
/// Resolves a wire entity name (from <c>[SencillaEntity]</c>) to its batch descriptor.
/// Built once at startup by scanning loaded assemblies.
/// </summary>
public interface IBatchEntityRegistry
{
    /// <summary>Looks up a descriptor by its <c>[SencillaEntity]</c> name (case-insensitive).</summary>
    bool TryGet(string entityName, out BatchEntityDescriptor descriptor);

    /// <summary>All registered descriptors.</summary>
    IReadOnlyCollection<BatchEntityDescriptor> All { get; }
}
