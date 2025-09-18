using Microsoft.Extensions.Options;
using System.IO;

namespace Sencilla.Component.Files.LocalDrive;

public class LocalDriveStorage(LocalDriveStorageOptions options) : IFileStorage
{
    public byte Type => options.Type;

    public string GetDirectory(string type, params object[] @params) => options.GetDirectory(type, @params);

    public string GetRootDirectory() => options.RootPath;

    public Stream OpenFileStream(File file, long offset = 0, CancellationToken? token = null)
    {
        var path = GetFilePath(file);

        CreateFileDirectory(path);

        var fs = new FileStream(path, FileMode.OpenOrCreate, FileAccess.ReadWrite);
        fs.Seek(offset, SeekOrigin.Begin);

        return fs;
    }

    public Stream OpenFileStream(string path, long offset = 0, CancellationToken? token = null)
    {
        path = Path.Combine(options.RootPath, path);
        CreateFileDirectory(path);

        var fs = new FileStream(path, FileMode.OpenOrCreate, FileAccess.ReadWrite);
        fs.Seek(offset, SeekOrigin.Begin);

        return fs;
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

    public Task<File?> DeleteFileAsync(File? file, CancellationToken? token = null)
    {
        if (file != null)
        {
            var path = GetFilePath(file);
            System.IO.File.Delete(path);
        }

        return Task.FromResult(file);
    }

    public async Task<bool> SaveFile(string file, Stream stream)
    {
        var fullPath = Path.Combine(options.RootPath ?? string.Empty, file);
        CreateFileDirectory(fullPath);

        if (stream.CanSeek)
            stream.Seek(0, SeekOrigin.Begin);

        // using var fs = new FileStream(fullPath, FileMode.Create, FileAccess.Write, FileShare.None);
        // stream.CopyTo(fs);
        
        int bufferSize = 81920;
        using var fs = new FileStream(fullPath, FileMode.Create, FileAccess.Write, FileShare.None, bufferSize: bufferSize, useAsync: true);
        await stream.CopyToAsync(fs, bufferSize, CancellationToken.None);

        return true;
    }

    public Task<bool> SaveFile(string file, byte[] data)
    {
        return SaveFile(file, new MemoryStream(data));
    }

    public Task<bool> CopyToFileAsync(File srcFile, string dstFile)
    {
        return CopyToFileAsync(GetFilePath(srcFile), dstFile);
    }

    public Task<bool> CopyToFileAsync(string srcPath, string dstPath)
    {
        // open file 
        var folder = Path.GetDirectoryName(dstPath);
        if (folder == null)
            return Task.FromResult(false);

        if (!Directory.Exists(folder))
            Directory.CreateDirectory(folder);

        System.IO.File.Copy(srcPath, dstPath, true);
        return Task.FromResult(true);
    }


    private string GetFilePath(File file)
    {
        var directory = Path.GetDirectoryName(file.Path) ?? string.Empty;
        var fileNameWithExt = Path.GetFileName(file.Path) ?? $"{file.Id}{Path.GetExtension(file.Name)}";
        return Path.Combine(options.RootPath, directory, fileNameWithExt);
    }

    private void CreateFileDirectory(string path)
    {
        var directory = Path.GetDirectoryName(path);
        if (directory?.Length > 0 && !Directory.Exists(directory))
            Directory.CreateDirectory(directory);
    }
}
