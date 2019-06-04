using Sencilla.Core.Component;
using Sencilla.Core.Injection;
using Sencilla.Component.Files.Entity;
using Sencilla.Component.Files.Impl;

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
            resolver.AddRepositoriesFor<File, long, FileDbContext>();
            resolver.AddRepositoriesFor<FileContent, long, FileDbContext>();
        }
    }
}
