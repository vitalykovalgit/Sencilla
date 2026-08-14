global using Microsoft.Extensions.Configuration;
global using Microsoft.Extensions.DependencyInjection;

global using Amazon;
global using Amazon.S3;
global using Amazon.S3.Model;
global using Amazon.S3.Transfer;

global using System.IO.Compression;

global using Sencilla.Component.Files;
global using Sencilla.Component.Files.AmazonS3;

namespace Microsoft.Extensions.DependencyInjection;

public static class Bootstrap
{
    public static SencillaFilesOptions AddAmazonS3(this SencillaFilesOptions root, Action<AmazonS3StorageOptions> configure)
    {
        var options = new AmazonS3StorageOptions { Bucket = "" };
        configure(options);
        return root.AddStorageInternal<AmazonS3Storage, AmazonS3StorageOptions>(options, null);
    }

    public static SencillaFilesOptions AddAmazonS3(this SencillaFilesOptions root, IConfigurationSection section)
    {
        var options = new AmazonS3StorageOptions { Bucket = "" };
        return root.AddStorageInternal<AmazonS3Storage, AmazonS3StorageOptions>(options, section: section);
    }

    /// <summary>
    /// Registers every configured S3 storage: the flat <c>SencillaFiles:AmazonS3</c> section, plus
    /// each entry under <c>SencillaFiles:Storages</c> whose <c>Provider</c> is <c>AmazonS3</c>.
    /// A second bucket — another Hetzner location, an AWS region — is therefore a configuration
    /// entry with its own <c>Type</c>, not a code change.
    /// </summary>
    public static SencillaFilesOptions AddAmazonS3(this SencillaFilesOptions root, IConfiguration configuration)
    {
        var flat = configuration.GetSection($"{root.Section}:{AmazonS3StorageOptions.SectionName}");
        if (flat.Exists())
        {
            var options = new AmazonS3StorageOptions { Bucket = "" };
            root.AddStorageInternal<AmazonS3Storage, AmazonS3StorageOptions>(options, configuration: configuration);
        }

        foreach (var instance in configuration.GetSection($"{root.Section}:Storages").GetChildren())
        {
            if (!string.Equals(instance["Provider"], AmazonS3StorageOptions.SectionName, StringComparison.OrdinalIgnoreCase))
                continue;

            var options = new AmazonS3StorageOptions { Bucket = "" };
            instance.Bind(options);

            // bindOwnSection: false — these values came from the named section; rebinding the flat
            // SencillaFiles:AmazonS3 section over them would overwrite this instance with the other one.
            root.AddStorageInternal<AmazonS3Storage, AmazonS3StorageOptions>(
                options, configuration: configuration, bindOwnSection: false);
        }

        return root;
    }
}
