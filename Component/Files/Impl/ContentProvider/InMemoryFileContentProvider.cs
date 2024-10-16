using System.Collections.Concurrent;
using System.IO;

namespace Sencilla.Component.Files;

public class InMemoryFileContentProvider : IFileContentProvider
{
    //private readonly ConcurrentDictionary<Guid, Stream> _files = new();

    private readonly IFileProvider _fileProvider;

    public InMemoryFileContentProvider(IFileProvider fileProvider)
    {
        _fileProvider = fileProvider;
    }

    public Task<File> DeleteFileAsync(File file, CancellationToken? token = null)
    {
        throw new NotImplementedException();
    }

    public Task<Stream> ReadFileAsync(File file, CancellationToken? token = null)
    {
        throw new NotImplementedException();
    }

    public Task<long> WriteFileAsync(File file, byte[] content, long offset = 0, CancellationToken? token = null)
    {
        //file.Position += content.LongLength;
        //await _fileProvider.UpdateFile(file);
        //return file.Position;
        //if (!_files.ContainsKey(file.Id))
        //    _files[file.Id] = new MemoryStream();

        //foreach (byte b in content)
        //    _files[file.Id].WriteByte(b);

        //return Task.FromResult(_files[file.Id].Position);
        return Task.FromResult(0L);
    }

    public Task<long> WriteFileAsync(File file, Stream stream, long offset = 0, long length = -1, CancellationToken? token = null)
    {
        //file.Position += stream.Length;
        //await _fileProvider.UpdateFile(file);
        //return file.Position;
        //await stream.CopyToAsync(_files[file.Id]);

        //return _files[file.Id].Position;
        return Task.FromResult(0L);
    }
}
