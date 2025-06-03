namespace Sencilla.Component.Files;

public class FileContentProviderOptions
{
    /// <summary>
    /// File content provider type. Available options defined in <see cref="FileContentProviderType "/> enum.
    /// </summary>
    public FileContentProviderType ContentProvider { get; set; }
    /// <summary>
    /// Used by Drive file content provider.
    /// </summary>
    public string RootPath { get; set; }
    public string ConnectionString { get; set; }
}
