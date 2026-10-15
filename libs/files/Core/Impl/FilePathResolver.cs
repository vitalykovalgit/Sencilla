namespace Sencilla.Component.Files;

[DisableInjection]
internal class FilePathResolver : IFilePathResolver
{
    public string GetFullPath(File file)
    {
        var fileDim = file.Dim == null ? "" : $"_{file.Dim}px";
        var fileName = $"{file.Id}{fileDim}{Path.GetExtension(file.Name)}";
        
        var projectId = file.Attrs?.GetString("projectId");
        var projectPath = projectId == null ? "" : $"project{projectId}";

        // The `folder` attr is a sub-folder of the owner's root (user{id}/templates/, system/templates/). Only letters,
        // digits, '_' and '-' survive, so it can never climb out of that root. Absent = no sub-folder.
        var folder = RemoveSpecialCharacters(file.Attrs?.GetString("folder"));
        var folderPath = folder.Length == 0 ? "" : $"{folder}/";

        return file.Origin switch
        {
            FileOrigin.User => Path.Combine($"user{file.UserId ?? Guid.Empty}", projectPath, folder, fileName),
            FileOrigin.System => $"system/{folderPath}{fileName}",
            _ => $"none/{folderPath}{fileName}"
        };
    }

    public string GetResolutionPath(File file, int res, string? contentType = null)
    {
        var resKey = res.ToString();

        // The recorded ResolutionInfo.Ct is the source of truth. A caller that is CREATING the derivative
        // has to pass its content type explicitly, because the Res entry is written after the path is needed.
        contentType ??= file.Res != null && file.Res.TryGetValue(resKey, out var info) ? info?.Ct : null;

        return GetResolutionPath(file.Path ?? GetFullPath(file), resKey, contentType);
    }

    public string GetResolutionPath(string path, string resKey, string? contentType = null)
    {
        if (string.IsNullOrEmpty(path)) return string.Empty;
        var ext = Path.GetExtension(path);
        var pathWithoutExt = path[..^ext.Length];
        return $"{pathWithoutExt}_{resKey}{ExtensionFor(contentType) ?? ext}";
    }

    /// <summary>
    /// Extension for a derivative's own content type, or null to keep the original's. Deliberately a short
    /// explicit table rather than a reverse lookup through FileExtensionContentTypeProvider, which maps one
    /// type to several extensions and would pick between .jpg/.jpe/.jpeg unpredictably.
    /// </summary>
    private static string? ExtensionFor(string? contentType)
    {
        if (string.IsNullOrWhiteSpace(contentType)) return null;

        // "image/jpeg; charset=..." — parameters are legal on any media type, so cut them off before matching.
        var semicolon = contentType.IndexOf(';');
        var type = (semicolon < 0 ? contentType : contentType[..semicolon]).Trim().ToLowerInvariant();

        return type switch
        {
            "image/webp" => ".webp",
            "image/jpeg" => ".jpg",
            "image/png" => ".png",
            "image/avif" => ".avif",
            "image/gif" => ".gif",
            "image/bmp" => ".bmp",
            "image/tiff" => ".tiff",
            "image/svg+xml" => ".svg",
            _ => null,
        };
    }

    /// <summary>
    /// Only ASCII letters, digits, '_' and '-' survive — what a path segment may hold. A plain check, not a table built
    /// on first construction: the resolver is transient, and two first constructions racing could read a half-built one.
    /// </summary>
    public static string RemoveSpecialCharacters(string? str) =>
        string.Concat((str ?? "").Where(c => char.IsAsciiLetterOrDigit(c) || c is '_' or '-'));
}
