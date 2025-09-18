global using Microsoft.Extensions.Configuration;
global using Microsoft.Extensions.DependencyInjection;
global using Microsoft.Extensions.DependencyInjection.Extensions;

global using Sencilla.Component.Files;
global using Sencilla.Component.Files.LocalDrive;

namespace Microsoft.Extensions.DependencyInjection;

public static class Bootstrap
{
    public static SencillaFilesOptions UseLocalDrive(this SencillaFilesOptions root, Action<LocalDriveStorageOptions> configure)
    {
        var options = new LocalDriveStorageOptions { RootPath = "" };
        configure(options);

        return root.AddStorageInternal<LocalDriveStorage, LocalDriveStorageOptions>(options);
    }

    public static SencillaFilesOptions UseLocalDrive(this SencillaFilesOptions root, IConfigurationSection section)
    {
        var options = new LocalDriveStorageOptions { RootPath = "" };
        return root.AddStorageInternal<LocalDriveStorage, LocalDriveStorageOptions>(options, section:section);
    }

    public static SencillaFilesOptions UseLocalDrive(this SencillaFilesOptions root, IConfiguration configuration, string? section = null)
    {
        var options = new LocalDriveStorageOptions { RootPath = "" };
        return root.AddStorageInternal<LocalDriveStorage, LocalDriveStorageOptions>(options, configuration:configuration);
    }
}
