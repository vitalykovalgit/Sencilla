namespace Sencilla.Component.Files;

public class InMemoryFileProvider : IFileProvider
{
    private readonly System.Collections.Concurrent.ConcurrentDictionary<Guid, File> _files = new();

    public Task<File> CreateFile(File file) => Task.FromResult(_files[file.Id] = file);

    public Task<File> GetFile(Guid fileId) => Task.FromResult(_files[fileId]);

    public Task<File> UpdateFile(File file) => Task.FromResult(_files[file.Id] = file);
}
