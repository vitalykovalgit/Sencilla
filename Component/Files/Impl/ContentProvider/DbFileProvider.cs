
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Sencilla.Component.Files.Entity;

namespace Sencilla.Component.Files.Impl.ContentProvider
{
    public class DbFileProvider : IFileContentProvider
    {
        public Task<Entity.File> DeleteFileAsync(Entity.File file, CancellationToken? token = null)
        {
            throw new System.NotImplementedException();
        }

        public Task<Stream> ReadFileAsync(CancellationToken? token = null)
        {
            throw new System.NotImplementedException();
        }

        public Task<Entity.File> WriteFileAsync(Entity.File file, byte[] content, CancellationToken? token = null)
        {
            throw new System.NotImplementedException();
        }

        public Task<Entity.File> WriteFileAsync(Entity.File file, Stream stream, CancellationToken? token = null)
        {
            throw new System.NotImplementedException();
        }
    }
}
