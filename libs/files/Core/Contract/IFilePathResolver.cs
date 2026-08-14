namespace Sencilla.Component.Files;

public interface IFilePathResolver
{
    string GetFullPath(File file);

    /// <summary>
    /// Path of a resolution derivative. The extension follows the DERIVATIVE's own content type, not the
    /// original's: a 100px variant of a .jpg is frequently re-encoded as WebP, and naming it .jpg makes the
    /// file lie to anything reading the storage directly.
    /// <paramref name="contentType"/> is required only when the caller writes a resolution whose
    /// <see cref="ResolutionInfo.Ct"/> is not recorded on <paramref name="file"/> yet; otherwise it is read
    /// from <see cref="File.Res"/>. Unknown or absent type keeps the original's extension.
    /// </summary>
    string GetResolutionPath(File file, int res, string? contentType = null);

    /// <inheritdoc cref="GetResolutionPath(File, int, string?)"/>
    string GetResolutionPath(string path, string resKey, string? contentType = null);
}
