global using Azure.Storage.Blobs;
global using Azure.Storage.Blobs.Specialized;
global using Microsoft.Extensions.DependencyInjection.Extensions;
global using Microsoft.Extensions.Configuration;
global using Microsoft.Extensions.DependencyInjection;

global using Sencilla.Component.Files;
global using Sencilla.Component.Files.AzureStorage;

namespace Microsoft.Extensions.DependencyInjection;

public static class Bootstrap
{
    public static SencillaFilesOptions UseAzureStorage(this SencillaFilesOptions root, Action<AzureBlobStorageOptions> configure)
    {
        var options = new AzureBlobStorageOptions { ConnectionString = "" };
        configure(options);
        return root.AddStorageInternal<AzureBlobStorage, AzureBlobStorageOptions>(options, null);
    }

    public static SencillaFilesOptions UseAzureStorage(this SencillaFilesOptions root, IConfigurationSection section)
    {
        var options = new AzureBlobStorageOptions { ConnectionString = "" };
        return root.AddStorageInternal<AzureBlobStorage, AzureBlobStorageOptions>(options, section: section);
    }

    public static SencillaFilesOptions UseAzureStorage(this SencillaFilesOptions root, IConfiguration configuration, string? section = null)
    {
        var options = new AzureBlobStorageOptions { ConnectionString = "" };
        return root.AddStorageInternal<AzureBlobStorage, AzureBlobStorageOptions>(options, configuration: configuration);
    }
}
