
using Sencilla.Core;
using Sencilla.Component.Config;
using Sencilla.Component.Files;

namespace Microsoft.Extensions.DependencyInjection
{
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
}
