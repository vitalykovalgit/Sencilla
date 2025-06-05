namespace Sencilla.Component.Files;

public class DriveFileContentProvider : IFileContentProvider
{
    public const FileContentProviderType ProviderType = FileContentProviderType.Drive;
    FileContentProviderType IFileContentProvider.ProviderType => ProviderType;

    private readonly FileContentProviderOptions _config;

    public DriveFileContentProvider(IConfigProvider<FileContentProviderOptions> config)
    {
        _config = config.GetConfig();
    }

    public Task<File?> DeleteFileAsync(File? file, CancellationToken? token = null)
    {
        if (file != null)
        {
            var path = GetFilePath(file);
            System.IO.File.Delete(path);
        }

        return Task.FromResult(file);
    }

    public Task<Stream?> ReadFileAsync(File file, CancellationToken? token = null) => Task.FromResult(ReadFile(file, token));

    public Stream? ReadFile(File file, CancellationToken? token = null)
    {
        var path = GetFilePath(file);

        Stream? stream = null;
        if (System.IO.File.Exists(path))
            stream = System.IO.File.OpenRead(path);

        return stream;
    }

    public async Task<long> WriteFileAsync(File file, byte[] content, long offset = 0, CancellationToken? token = null)
    {
        var path = GetFilePath(file);

        CreateFileDirectory(path);

        await System.IO.File.WriteAllBytesAsync(path, content);

        return new FileInfo(path).Length;
    }

    public async Task<long> WriteFileAsync(File file, Stream stream, long offset = 0, long length = -1, CancellationToken? token = null)
    {
        var path = GetFilePath(file);

        CreateFileDirectory(path);

        using var fs = new FileStream(path, FileMode.OpenOrCreate, FileAccess.Write);
        fs.Seek(offset, SeekOrigin.Begin);

        await stream.CopyToAsync(fs, token ?? CancellationToken.None);

        long newOffset = fs.Position;

        return newOffset;
    }

    public Stream OpenFileStream(File file, long offset = 0, CancellationToken? token = null)
    {
        var path = GetFilePath(file);

        CreateFileDirectory(path);

        var fs = new FileStream(path, FileMode.OpenOrCreate, FileAccess.ReadWrite);
        fs.Seek(offset, SeekOrigin.Begin);

        return fs;
    }

    private string GetFilePath(File file)
    {
        var rootPath = _config.RootPath ?? string.Empty;
        var directory = Path.GetDirectoryName(file.FullName) ?? string.Empty;
        var fileNameWithExt = Path.GetFileName(file.FullName) ?? $"{file.Id}{file.Extension ?? Path.GetExtension(file.Name)}";

        return Path.Combine(rootPath, directory, fileNameWithExt);
    }

    private void CreateFileDirectory(string path)
    {
        var directory = Path.GetDirectoryName(path);
        if (directory?.Length > 0 && !Directory.Exists(directory))
            Directory.CreateDirectory(directory);
    }
}
