namespace Sencilla.Component.Files.LocalDrive;

public class LocalDriveStorageOptions: BaseFilesOptions
{
    /// <summary>
    /// File content provider type. Available options defined in <see cref="FileContentProviderType "/> enum.
    /// </summary>
    public override byte Type => 1;
    public override string Section => "LocalDrive";

    /// <summary>
    /// Used by Drive file content provider.
    /// </summary>
    public required string RootPath { get; set; }
}
