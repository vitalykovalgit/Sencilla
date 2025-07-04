﻿namespace Sencilla.Component.Files;

/// <summary>
/// Save file and retrieve to/from storage
/// </summary>
public interface IFileContentProvider
{
    FileContentProviderType ProviderType { get; }
    Stream? ReadFile(File file, CancellationToken? token = null);
    Task<Stream?> ReadFileAsync(File file, CancellationToken? token = null);

    Task<long> WriteFileAsync(File file, byte[] content, long offset = 0, CancellationToken? token = null);
    Task<long> WriteFileAsync(File file, Stream stream, long offset = 0, long length = -1, CancellationToken? token = null);
    
    Stream OpenFileStream(File file, long offset = 0, CancellationToken? token = null);
    
    Task<File?> DeleteFileAsync(File? file, CancellationToken? token = null);
    
    /// <summary>
    /// Think about this method here 
    /// </summary>
    /// <param name="type"></param>
    /// <returns></returns>
    static string ServiceKey(FileContentProviderType type) => $"{nameof(IFileContentProvider)}.{type}";
}
