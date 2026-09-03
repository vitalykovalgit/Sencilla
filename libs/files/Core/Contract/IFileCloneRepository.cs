namespace Sencilla.Component.Files;

/// <summary>
/// Copies file rows together with their blobs into a new owner and/or attribute scope — the mirror
/// of <c>DeleteFileHandler</c>'s walk: each file's resolution variants and its <c>ParentId</c>
/// children travel with it.
/// </summary>
public interface IFileCloneRepository
{
    /// <summary>
    /// Clones <paramref name="fileIds"/> and every child hanging off them. Returns old id → new id
    /// for each row written, children included.
    ///
    /// <paramref name="userId"/> and <paramref name="attrs"/> are inputs rather than something the
    /// caller patches afterwards because <see cref="IFilePathResolver.GetFullPath"/> derives the
    /// blob path from both — the destination is not known until they are. <paramref name="attrs"/>
    /// is merged over each source's own attributes; everything else is copied verbatim.
    ///
    /// Rows go through the ordinary create repository, so a transaction the caller has open covers
    /// them. Blob copies do NOT participate: a rolled-back clone leaves its blobs behind for the
    /// caller's wipe/retry path to clear.
    /// </summary>
    Task<IReadOnlyDictionary<Guid, Guid>> Clone(
        IReadOnlyCollection<Guid> fileIds,
        Guid? userId = null,
        IDictionary<string, string>? attrs = null,
        CancellationToken token = default);
}
