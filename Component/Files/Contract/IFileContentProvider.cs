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
        Task<Stream> ReadFileAsync(Entity.File file, CancellationToken? token = null);

        Task<Entity.File> WriteFileAsync(Entity.File file, byte[] content, CancellationToken? token = null);

        Task<Entity.File> WriteFileAsync(Entity.File file, Stream stream, CancellationToken? token = null);

        Task<Entity.File> DeleteFileAsync(Entity.File file, CancellationToken? token = null);
    }
}
