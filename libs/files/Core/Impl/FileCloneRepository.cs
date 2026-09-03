using Microsoft.Extensions.Logging;

namespace Sencilla.Component.Files;

[DisableInjection]
internal class FileCloneRepository(
    IFileStorage storage,
    IFilePathResolver pathResolver,
    ILogger<FileCloneRepository> logger,
    IReadRepository<File, Guid> files,
    ICreateRepository<File, Guid> create) : IFileCloneRepository
{
    public async Task<IReadOnlyDictionary<Guid, Guid>> Clone(
        IReadOnlyCollection<Guid> fileIds,
        Guid? userId = null,
        IDictionary<string, string>? attrs = null,
        CancellationToken token = default)
    {
        var ids = fileIds.Distinct().ToArray();
        if (ids.Length == 0)
            return new Dictionary<Guid, Guid>();

        var parents = (await files.GetAll(new FileFilter().ById(ids), token)).ToList();

        // Derivative rows hang off ParentId and belong to their original, so they come along
        // without being asked for. A requested id that is also someone's child is not cloned twice.
        var parentIds = parents.Select(p => p.Id).ToHashSet();
        var children = (await files.GetAll(new FileFilter().ByParentId(ids), token))
            .Where(c => !parentIds.Contains(c.Id))
            .ToList();

        var map = parents.Concat(children).ToDictionary(f => f.Id, _ => Guid.NewGuid());
        var clones = parents.Concat(children).Select(src => (Source: src, Clone: CloneOf(src, map, userId, attrs))).ToList();

        // Blobs first, then every row: a caller holding a transaction open keeps it off the slow
        // blob IO, and an interrupted clone leaves orphan blobs rather than rows pointing at nothing.
        foreach (var (source, clone) in clones)
            await CopyBlobsAsync(source, clone, token);

        // Parents before children — ParentId is a self-FK.
        foreach (var (_, clone) in clones)
            await create.Create(clone, token);

        return map;
    }

    private File CloneOf(File src, IReadOnlyDictionary<Guid, Guid> map, Guid? userId, IDictionary<string, string>? attrs)
    {
        var now = DateTime.UtcNow;
        var clone = new File
        {
            Id = map[src.Id],

            // A child follows its parent's clone. A file whose own parent is outside the copied set
            // is detached rather than left pointing back into the source's tree.
            ParentId = src.ParentId is Guid parentId && map.TryGetValue(parentId, out var cloned) ? cloned : null,

            Name = src.Name,
            MimeType = src.MimeType,
            Size = src.Size,
            Uploaded = src.Uploaded,
            UserId = userId ?? src.UserId,
            Origin = src.Origin,
            Storage = src.Storage,
            Dim = src.Dim,
            Width = src.Width,
            Height = src.Height,

            // Copied, not shared: two clones must never end up holding the same dictionary instance.
            // Ct rides along, which is what keeps the variant paths below resolvable.
            Res = src.Res == null ? null : new Dictionary<string, ResolutionInfo>(src.Res),
            Attrs = Merge(src.Attrs, attrs),

            CreatedDate = now,
            UpdatedDate = now,
            DeletedDate = src.DeletedDate,
        };

        clone.Path = pathResolver.GetFullPath(clone);
        return clone;
    }

    private static IDictionary<string, string>? Merge(IDictionary<string, string>? source, IDictionary<string, string>? overrides)
    {
        if (source == null && overrides == null)
            return null;

        var merged = source == null ? [] : new Dictionary<string, string>(source);

        // Attribute keys are lower-cased everywhere else in the component — GetMetadata lower-cases
        // on the way in, GetString lower-cases on the way out — so an override keyed "projectId"
        // would sit next to the stored "projectid" instead of replacing it, and GetFullPath would
        // keep resolving the ORIGINAL value.
        foreach (var attr in overrides ?? new Dictionary<string, string>())
            merged[attr.Key.ToLower()] = attr.Value;

        return merged;
    }

    private async Task CopyBlobsAsync(File source, File clone, CancellationToken token)
    {
        await CopyBlobAsync(source.Path ?? pathResolver.GetFullPath(source), clone.Path!, token);

        // Each variant is copied at ITS OWN content type: GetResolutionPath reads ResolutionInfo.Ct,
        // so a WebP derivative of a JPEG original resolves to _{res}.webp on both sides. Building
        // these paths from the raw path instead would look for _{res}.jpg and find nothing.
        foreach (var resKey in source.Res?.Keys.ToList() ?? [])
            if (int.TryParse(resKey, out var res))
                await CopyBlobAsync(pathResolver.GetResolutionPath(source, res), pathResolver.GetResolutionPath(clone, res), token);
    }

    private async Task CopyBlobAsync(string sourcePath, string destinationPath, CancellationToken token)
    {
        // Deliberately not IFileStorage.CopyToFileAsync — the Azure implementation of it writes to
        // the local filesystem (Directory.CreateDirectory + File.OpenWrite), so it would put the
        // copy on the pod's disk instead of in blob storage.
        var stream = await storage.ReadFileAsync(sourcePath, token);
        if (stream == null)
        {
            // A missing source is mirrored rather than retried forever: the clone ends up as broken
            // as the original it came from, which is recoverable — a permanently failing clone is not.
            logger.LogWarning("Clone: source blob {Path} is missing — skipped", sourcePath);
            return;
        }

        await using (stream)
            await storage.SaveFile(destinationPath, stream);
    }
}
