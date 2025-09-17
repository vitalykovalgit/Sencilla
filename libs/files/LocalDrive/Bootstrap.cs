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

        return AddLocalDrive(root, options, null);
    }

    public static SencillaFilesOptions UseLocalDrive(this SencillaFilesOptions root, IConfiguration configuration, string? section = null)
    {
        var options = new LocalDriveStorageOptions { RootPath = "" };
        configuration.GetSection(section ?? $"{root.Section}:{options.Section}").Bind(options);

        // Read dirs from root path 
        var optionsDirs = new LocalDriveStorageOptions { RootPath = "" };
        configuration.GetSection($"{root.Section}:Dirs").Bind(optionsDirs.Dirs);

        return AddLocalDrive(root, options, optionsDirs);
    }

    private static SencillaFilesOptions AddLocalDrive(SencillaFilesOptions root, LocalDriveStorageOptions options, BaseFilesOptions? dirs)
    {
        // merge directories 
        foreach (var kvp in root.Dirs)
            options.Dirs.TryAdd(kvp.Key, kvp.Value);

        if (dirs != null)
        {
            foreach (var kvp in dirs.Dirs)
                options.Dirs.TryAdd(kvp.Key, kvp.Value);
        }

        root.Services.TryAddKeyedTransient<IFileStorage, LocalDriveStorage>(options.Type);
        root.Services.TryAddSingleton(options);

        if (options.UseAsDefault)
            root.Services.TryAddTransient<IFileStorage, LocalDriveStorage>();

        return root;
    }

}
