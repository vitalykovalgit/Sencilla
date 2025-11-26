namespace Sencilla.Component.Files;

[DisableInjection]
public class FileUploadOptions
{
    /// <summary>
    /// 
    /// </summary>
    public required string Route { get; set; }

    /// <summary>
    /// 
    /// </summary>
    public Func<File, string>? GetPathHandler { get; set; }
}
