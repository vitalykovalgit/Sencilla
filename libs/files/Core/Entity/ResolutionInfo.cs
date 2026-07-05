namespace Sencilla.Component.Files;

/// <summary>
/// Tracks upload progress for a single resolution variant.
/// During upload: S = total size, U = uploaded bytes.
/// After upload complete: both null (serialized as empty object {}).
/// </summary>
public class ResolutionInfo
{
    /// <summary>
    /// Total size in bytes (null when upload is complete)
    /// </summary>
    public long? S { get; set; }

    /// <summary>
    /// Uploaded bytes (null when upload is complete)
    /// </summary>
    public long? U { get; set; }

    /// <summary>Per-variant content type / format (e.g. "image/webp","image/png","image/jpeg").
    /// Null for rows created before this field existed → callers fall back to File.MimeType.</summary>
    public string? Ct { get; set; }
}
