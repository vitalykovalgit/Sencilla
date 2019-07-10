using Sencilla.Core.Component;
using Sencilla.Core.Injection;
using Sencilla.Component.Files.Entity;
using Sencilla.Component.Files.Impl;
using Sencilla.Component.Files.Impl.ContentProvider;
using Sencilla.Component.Config.Contract;
using Sencilla.Component.Config.Impl;

namespace Sencilla.Component.Files
{
    /// <summary>
    /// 
    /// </summary>
    public class FilesComponent : IComponent
    {
        public string Type => "Sencilla.Component.Files";

        public void Init(IResolver resolver)
        {
            resolver.AddRepositoriesFor<File, ulong, FileDbContext>();
            resolver.AddRepositoriesFor<FileContent, ulong, FileDbContext>();

            resolver.RegisterType<IFileContentProvider, DriveFileProvider>();

            resolver.RegisterType<IConfigProvider<DriveFileProviderOption>, AppSettingsJsonConfigProvider<DriveFileProviderOption>>();
        }
    }
}
