﻿namespace Sencilla.Component.Files;

public class DbFileContentProvider : IFileContentProvider
{
    public const FileContentProviderType ProviderType = FileContentProviderType.Db;
    FileContentProviderType IFileContentProvider.ProviderType => ProviderType;

    public Task<Stream> ReadFileAsync(File file, CancellationToken? token = null)
    {
        throw new System.NotImplementedException();
    }

    public Task<File?> DeleteFileAsync(File? file, CancellationToken? token = null)
    {
        throw new System.NotImplementedException();
    }

    public Task<long> WriteFileAsync(File file, byte[] content, long offset = 0, CancellationToken? token = null)
    {
        throw new System.NotImplementedException();
    }

    public Task<long> WriteFileAsync(File file, Stream stream, long offset = 0, long length = -1, CancellationToken? token = null)
    {
        throw new System.NotImplementedException();
    }

    public Stream OpenFileStream(File file, long offset = 0, CancellationToken? token = null)
    {
        throw new NotImplementedException();
    }

    public Stream ReadFile(File file, CancellationToken? token = null)
    {
        throw new NotImplementedException();
    }
}
