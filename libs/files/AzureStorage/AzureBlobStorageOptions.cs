namespace Sencilla.Component.Files.AzureStorage;

public class AzureBlobStorageOptions: BaseFilesOptions
{
    /// <summary>
    /// File content provider type. Available options defined in <see cref="FileContentProviderType "/> enum.
    /// </summary>
    public override byte Type => 2;
    public override string Section => "AzureStorage";

    /// <summary>
    /// 
    /// </summary>
    public required string ConnectionString { get; set; }
}
