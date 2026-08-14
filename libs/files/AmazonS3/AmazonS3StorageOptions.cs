namespace Sencilla.Component.Files.AmazonS3;

public class AmazonS3StorageOptions: BaseFilesOptions
{
    public const string SectionName = "AmazonS3";

    /// <summary>
    /// Storage instance id — 3 for the first S3 storage. Give additional S3 instances their own
    /// id (4, 5, …) so <c>File.Storage</c> can tell the buckets apart.
    /// </summary>
    public override byte Type { get; set; } = 3;
    public override string Section => SectionName;

    /// <summary>
    /// Access key id.
    /// </summary>
    public string? AccessKey { get; set; }

    /// <summary>
    /// Secret key.
    /// </summary>
    public string? SecretKey { get; set; }

    /// <summary>
    /// AWS region (for example: eu-central-1). Ignored when <see cref="ServiceUrl"/> is set —
    /// the AWS SDK treats ServiceURL and RegionEndpoint as mutually exclusive.
    /// </summary>
    public string? Region { get; set; }

    /// <summary>
    /// Endpoint of an S3-compatible service that is not AWS — Hetzner
    /// (<c>https://fsn1.your-objectstorage.com</c>), MinIO, Cloudflare R2.
    /// Leave empty for AWS and set <see cref="Region"/> instead.
    /// </summary>
    public string? ServiceUrl { get; set; }

    /// <summary>
    /// SigV4 signing region used with <see cref="ServiceUrl"/>. Hetzner expects its location
    /// name (fsn1 / nbg1 / hel1); most other S3 clones accept us-east-1.
    /// </summary>
    public string? AuthenticationRegion { get; set; }

    /// <summary>
    /// Address buckets as <c>{endpoint}/{bucket}/{key}</c> instead of
    /// <c>{bucket}.{endpoint}/{key}</c>. Needed by MinIO and by any endpoint whose TLS
    /// certificate does not cover the bucket sub-domain.
    /// </summary>
    public bool ForcePathStyle { get; set; }

    /// <summary>
    /// The one bucket this storage instance writes to. Buckets are never derived from the file
    /// path and are never created implicitly.
    /// </summary>
    public required string Bucket { get; set; }

    /// <summary>
    /// Prefix prepended to every key, e.g. <c>dev/</c>. Keeps <c>File.Path</c> byte-identical
    /// across environments, so a database restored from production into a lower environment
    /// resolves to that environment's own prefix and finds nothing, instead of silently reading
    /// (and deleting) production objects.
    /// </summary>
    public string? KeyPrefix { get; set; }

    /// <summary>
    /// Multipart part size, and therefore the chunk size a resumable client MUST send. S3
    /// requires every part except the last to be at least 5 MiB, so a smaller client chunk
    /// cannot be completed — see <see cref="S3Paths.MinPartSize"/>.
    /// </summary>
    public int PartSize { get; set; } = 8 * 1024 * 1024;
}
