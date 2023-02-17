
global using Sencilla.Core;
global using Sencilla.Component.Config;
global using Sencilla.Component.Files;

[assembly: AutoDiscovery]

namespace Microsoft.Extensions.DependencyInjection;

/// <summary>
/// 
/// </summary>
public class FilesComponent : IComponent
{
    public string Type => "Sencilla.Component.Files";

    public void Init(IContainer container)
    {
        //container.AddRepositoriesFor<File, ulong, FileDbContext>();
        //container.AddRepositoriesFor<FileContent, ulong, FileDbContext>();

        container.RegisterType<IFileContentProvider, DriveFileProvider>();
        container.RegisterType<IConfigProvider<DriveFileProviderOption>, AppSettingsJsonConfigProvider<DriveFileProviderOption>>();
    }
}
