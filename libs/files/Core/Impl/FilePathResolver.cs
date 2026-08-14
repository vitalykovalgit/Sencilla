namespace Sencilla.Component.Files;

[DisableInjection]
internal class FilePathResolver : IFilePathResolver
{
    private static bool[]? _lookup;

    public FilePathResolver() 
    {
        if (_lookup == null)
        {
            _lookup = new bool[65536];
            for (char c = '0'; c <= '9'; c++) _lookup[c] = true;
            for (char c = 'A'; c <= 'Z'; c++) _lookup[c] = true;
            for (char c = 'a'; c <= 'z'; c++) _lookup[c] = true;
            //_lookup['.'] = true;
            _lookup['_'] = true;
            _lookup['-'] = true;
        }

    }

    public string GetFullPath(File file)
    {
        var fileDim = file.Dim == null ? "" : $"_{file.Dim}px";
        var fileName = $"{file.Id}{fileDim}{Path.GetExtension(file.Name)}";
        
        var projectId = file.Attrs?.GetString("projectId");
        var projectPath = projectId == null ? "" : $"project{projectId}";
        //var folder = file.Attrs?.GetString("folder");
        //var folderPath = RemoveSpecialCharacters(folder); // remove any special characters leves only latters and numbers 

        return file.Origin switch
        {
            FileOrigin.User => Path.Combine($"user{file.UserId ?? Guid.Empty}", projectPath,/* folderPath,*/ fileName),
            FileOrigin.System => $"system/{fileName}",
            _ => $"none/{fileName}"
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

    public static string RemoveSpecialCharacters(string? str)
    {
        if (string.IsNullOrWhiteSpace(str))
            return string.Empty;

        char[] buffer = new char[str.Length];
        int index = 0;
        foreach (char c in str)
        {
            if (_lookup![c])
            {
                buffer[index] = c;
                index++;
            }
        }
        return new string(buffer, 0, index);
    }
}
