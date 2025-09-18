global using Azure.Storage.Blobs;
global using Azure.Storage.Blobs.Specialized;
global using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Configuration;

namespace Sencilla.Component.Files.AzureStorage;

public static class Bootstrap
{
    public static SencillaFilesOptions UseAzureStorage(this SencillaFilesOptions root, Action<AzureBlobStorageOptions> configure)
    {
        var options = new AzureBlobStorageOptions { ConnectionString = "" };
        configure(options);
        return AddAzureStorage(root, options);
    }

    public static SencillaFilesOptions UseAzureStorage(this SencillaFilesOptions root, IConfigurationSection section)
    {
        var options = new AzureBlobStorageOptions { ConnectionString = "" };
        section.GetSection($"{root.Section}:{options.Section}").Bind(options);
        return AddAzureStorage(root, options);
    }

    public static SencillaFilesOptions UseAzureStorage(this SencillaFilesOptions root, IConfiguration configuration, string? section = null)
    {
        var options = new AzureBlobStorageOptions { ConnectionString = "" };
        configuration.GetSection(section ?? $"{root.Section}:{options.Section}").Bind(options);
        return AddAzureStorage(root, options);
    }

    private static SencillaFilesOptions AddAzureStorage(SencillaFilesOptions root, AzureBlobStorageOptions options)
    {
        // merge directories 
        foreach (var kvp in root.Dirs)
            options.Dirs.TryAdd(kvp.Key, kvp.Value);

        root.Services.TryAddKeyedTransient<IFileStorage, AzureBlobStorage>(options.Type);
        root.Services.TryAddSingleton(options);

        if (options.UseAsDefault)
            root.Services.TryAddTransient<IFileStorage, AzureBlobStorage>();

        return root;
    }

}
