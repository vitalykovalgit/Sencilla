
global using Sencilla.Core;
global using Sencilla.Component.Config;
global using Sencilla.Component.Files;
global using Sencilla.Web;

global using Microsoft.AspNetCore.Http;
global using Microsoft.AspNetCore.Mvc;


global using Azure.Storage.Blobs;
global using Azure.Storage.Blobs.Specialized;

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

        container.RegisterType<IFileContentProvider, AzureBlobStorageContentProvider>(
            IFileContentProvider.ServiceKey(AzureBlobStorageContentProvider.ProviderType));
        container.RegisterType<IFileContentProvider, DriveFileContentProvider>(
            IFileContentProvider.ServiceKey(DriveFileContentProvider.ProviderType));

        container.RegisterType<IConfigProvider<FileContentProviderOptions>, AppSettingsJsonConfigProvider<FileContentProviderOptions>>();

        container.RegisterType(provider => provider.Resolve<IFileContentProvider>(IFileContentProvider.ServiceKey(provider.Resolve<IConfigProvider<FileContentProviderOptions>>()!.GetConfig().ContentProvider))!);

        container.RegisterType<IFileRepository, DbFileRepository>();
        container.RegisterType<IFileUploadRepository, DbFileUploadRepository>();
    }
}
