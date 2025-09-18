namespace Sencilla.Component.Files.AmazonS3;

public class AmazonS3StorageOptions: BaseFilesOptions
{
    public override byte Type => 3;
    public override string Section => "AmazonS3";

    /// <summary>
    /// AWS access key id
    /// </summary>
    public string? AccessKey { get; set; }

    /// <summary>
    /// AWS secret key
    /// </summary>
    public string? SecretKey { get; set; }

    /// <summary>
    /// AWS region (for example: us-east-1)
    /// </summary>
    public string? Region { get; set; }

    /// <summary>
    /// Default bucket name
    /// </summary>
    public required string Bucket { get; set; }
}
