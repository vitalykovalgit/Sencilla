
global using Sencilla.Core;
global using Sencilla.Component.Config;
global using Sencilla.Component.Files;

global using System.Text;
global using System.Net.Mime;

global using Microsoft.AspNetCore.Http;
global using Microsoft.AspNetCore.Builder;
global using Microsoft.Extensions.DependencyInjection;

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

        container.RegisterType<IFileProvider, DbFileProvider>();

        // Tus Resumable Upload
        container.RegisterType<ITusRequestHandler, CreateFileHandler>(nameof(CreateFileHandler));
        container.RegisterType<ITusRequestHandler, UploadFileHandler>(nameof(UploadFileHandler));
    }
}
