
global using Sencilla.Core;
global using Sencilla.Component.Config;
global using Sencilla.Component.Files;

global using Microsoft.AspNetCore.Http;

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

        container.RegisterType<IFileContentProvider, DriveFileContentProvider>();
        container.RegisterType<IConfigProvider<DriveFileContentProviderOption>, AppSettingsJsonConfigProvider<DriveFileContentProviderOption>>();

        container.RegisterType<IFileRepository, DbFileRepository>();
        container.RegisterType<IFileUploadRepository, DbFileUploadRepository>();
    }
}
