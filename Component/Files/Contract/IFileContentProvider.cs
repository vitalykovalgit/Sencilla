namespace Sencilla.Component.Files;

/// <summary>
/// Save file and retrieve to/from storage 
/// </summary>
public interface IFileContentProvider
{
    Task<Stream> ReadFileAsync(File file, CancellationToken? token = null);

    Task<long> WriteFileAsync(File file, byte[] content, long offset = 0, CancellationToken? token = null);

    Task<long> WriteFileAsync(File file, Stream stream, long offset = 0, CancellationToken? token = null);

    Task<File> DeleteFileAsync(File file, CancellationToken? token = null);
}
