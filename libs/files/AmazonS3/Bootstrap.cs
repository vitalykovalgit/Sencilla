global using Microsoft.Extensions.Configuration;
global using Microsoft.Extensions.DependencyInjection;

global using Amazon;
global using Amazon.S3;
global using Amazon.S3.Model;

global using Sencilla.Component.Files;
global using Sencilla.Component.Files.AmazonS3;

namespace Microsoft.Extensions.DependencyInjection;

public static class Bootstrap
{
    public static SencillaFilesOptions UseAmazonS3(this SencillaFilesOptions root, Action<AmazonS3StorageOptions> configure)
    {
        var options = new AmazonS3StorageOptions { Bucket = "" };
        configure(options);
        return root.AddStorageInternal<AmazonS3Storage, AmazonS3StorageOptions>(options, null);
    }

    public static SencillaFilesOptions UseAmazonS3(this SencillaFilesOptions root, IConfigurationSection section)
    {
        var options = new AmazonS3StorageOptions { Bucket = "" };
        return root.AddStorageInternal<AmazonS3Storage, AmazonS3StorageOptions>(options, section: section);
    }

    public static SencillaFilesOptions UseAmazonS3(this SencillaFilesOptions root, IConfiguration configuration)
    {
        var options = new AmazonS3StorageOptions { Bucket = "" };
        return root.AddStorageInternal<AmazonS3Storage, AmazonS3StorageOptions>(options, configuration: configuration);
    }
}

