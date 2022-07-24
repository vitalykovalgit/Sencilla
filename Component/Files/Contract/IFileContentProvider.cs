using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Sencilla.Component.Files
{
    /// <summary>
    /// Save file and retrieve to/from storage 
    /// </summary>
    public interface IFileContentProvider
    {
        Task<Stream> ReadFileAsync(File file, CancellationToken? token = null);

        Task<File> WriteFileAsync(File file, byte[] content, CancellationToken? token = null);

        Task<File> WriteFileAsync(File file, Stream stream, CancellationToken? token = null);

        Task<File> DeleteFileAsync(File file, CancellationToken? token = null);
    }
}
