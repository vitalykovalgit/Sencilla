
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Sencilla.Component.Files.Impl.ContentProvider
{
    public class DbFileProvider : IFileContentProvider
    {
        public Task<Stream> ReadFileAsync(File file, CancellationToken? token = null)
        {
            throw new System.NotImplementedException();
        }

        public Task<File> DeleteFileAsync(File file, CancellationToken? token = null)
        {
            throw new System.NotImplementedException();
        }

        public Task<File> WriteFileAsync(File file, byte[] content, CancellationToken? token = null)
        {
            throw new System.NotImplementedException();
        }

        public Task<File> WriteFileAsync(File file, Stream stream, CancellationToken? token = null)
        {
            throw new System.NotImplementedException();
        }
    }
}
