
using Sencilla.Component.Files.Impl.ContentProvider;
using Sencilla.Component.Config.Contract;
using Sencilla.Component.Config.Impl;
using Sencilla.Core;

namespace Sencilla.Component.Files
{
    /// <summary>
    /// 
    /// </summary>
    public class FilesComponent : IComponent
    {
        public string Type => "Sencilla.Component.Files";

        public void Init(IRegistrator container)
        {
            //container.AddRepositoriesFor<File, ulong, FileDbContext>();
            //container.AddRepositoriesFor<FileContent, ulong, FileDbContext>();

            container.RegisterType<IFileContentProvider, DriveFileProvider>();
            container.RegisterType<IConfigProvider<DriveFileProviderOption>, AppSettingsJsonConfigProvider<DriveFileProviderOption>>();
        }
    }
}
