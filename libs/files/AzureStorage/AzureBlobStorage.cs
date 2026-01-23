namespace Sencilla.Component.Files.AzureStorage;

public class AzureBlobStorage(AzureBlobStorageOptions options) : IFileStorage
{
    private readonly BlobServiceClient BlobServiceClient = new BlobServiceClient(options.ConnectionString);

    public byte Type => options.Type;

    public string GetDirectory(string type, params object[] @params) => options.GetDirectory(type, @params);
    public string GetRootDirectory() => "/";

    public Stream OpenFileStream(File file, long offset = 0, CancellationToken token = default)
    {
        var (containerName, blobName) = GetContainerAndFileName(file);
        var blobContainerClient = GetContainerClient(containerName, blobName);

        var client = blobContainerClient.GetBlobClient(blobName);
        return client.OpenWrite(true, null, token);
    }

    public Stream OpenFileStream(string path, long offset = 0, CancellationToken token = default)
    {
        var (containerName, blobName) = GetContainerAndFileName(path);
        var blobContainerClient = GetContainerClient(containerName, blobName);

        var client = blobContainerClient.GetBlobClient(blobName);
        return client.OpenWrite(true, null, token);
    }

    public async Task<Stream?> ReadFileAsync(File file, CancellationToken token = default)
    {
        var (containerName, blobName) = GetContainerAndFileName(file);

        var blobContainerClient = GetContainerClient(containerName, blobName);
        var blobClient = blobContainerClient.GetBlobClient(blobName);

        var downloadResponse = await blobClient.DownloadStreamingAsync(null, token);

        // System.NotSupportedException: Specified method is not supported.
        //  at Azure.Core.Pipeline.RetriableStream.RetriableStreamImpl.get_Length()
        //  at OpenCvSharp.Mat.FromStream(Stream stream, ImreadModes mode)
        //return downloadResponse.Value.Content;

        //var ms = new MemoryStream();
        //await downloadResponse.Value.Content.CopyToAsync(ms);
        //ms.Position = 0;
        //return ms;
        return downloadResponse.Value.Content;
    }

    public async Task<Stream?> ReadFileAsync(string file, CancellationToken token = default)
    {
        var (containerName, blobName) = GetContainerAndFileName(file);

        var blobContainerClient = GetContainerClient(containerName, blobName);
        var blobClient = blobContainerClient.GetBlobClient(blobName);

        var downloadResponse = await blobClient.DownloadStreamingAsync(null, token);
        return downloadResponse.Value.Content;
    }

    public async Task<long> WriteFileAsync(File file, byte[] content, long offset = 0, CancellationToken token = default)
    {
        using var ms = new MemoryStream(content);
        return await WriteStreamToBlobAsync(file, ms, offset, -1, token);
    }

    public Task<long> WriteFileAsync(File file, Stream stream, long offset = 0, long length = -1, CancellationToken token = default) =>
        WriteStreamToBlobAsync(file, stream, offset, length, token);


    public Task ZipFolderAsync(string folderToArchive, string destinationFile, CancellationToken token = default)
    {
        throw new NotImplementedException();
    }

    public Task<File?> DeleteFileAsync(File? file, CancellationToken token = default)
    {
        throw new NotImplementedException();
    }

    private async Task<long> WriteStreamToBlobAsync(File file, Stream stream, long offset = 0, long length = -1, CancellationToken? token = null)
    {
        var (containerName, blobName) = GetContainerAndFileName(file);

        var container = GetContainerClient(containerName, blobName);
        await container.CreateIfNotExistsAsync();

        var appendBlobClient = container.GetAppendBlobClient(blobName);
        await appendBlobClient.CreateIfNotExistsAsync();

        using var ms = new MemoryStream();
        if (!stream.CanSeek)
        {
            await stream.CopyToAsync(ms);
            ms.Position = 0;
            stream = ms;
        }

        if (stream.Length == 0)
            return 0;

        var maxBlockSize = appendBlobClient.AppendBlobMaxAppendBlockBytes;
        long commitedOffset = 0, lastBlockSize = 0, bytesLeft = stream.Length;
        var buffer = new byte[bytesLeft];

        while (bytesLeft > 0)
        {
            int blockSize = (int)Math.Min(bytesLeft, maxBlockSize);
            int bytesRead = await stream.ReadAsync(buffer.AsMemory(0, blockSize));
            await using var memoryStream = new MemoryStream(buffer, 0, bytesRead);
            var appendResponse = await appendBlobClient.AppendBlockAsync(memoryStream, cancellationToken: token ?? CancellationToken.None);
            bytesLeft -= bytesRead;
            lastBlockSize = bytesRead;
            commitedOffset = long.Parse(appendResponse.Value.BlobAppendOffset);
        }

        return commitedOffset + lastBlockSize;
    }

    public async Task<bool> SaveFile(string file, Stream stream)
    {
        return await WriteStreamToBlobAsync(new File { Path = file }, stream) > 0;
    }

    public Task<bool> SaveFile(string file, byte[] data)
    {
        using var ms = new MemoryStream(data);
        return SaveFile(file, ms);
    }

    public Task<bool> CopyToFileAsync(File srcFile, string dstPath)
    {
        return CopyToFileAsync(srcFile.Path, dstPath);
    }

    public async Task<bool> CopyToFileAsync(string? srcPath, string dstPath)
    {
        if (srcPath == null)
            return false;

        var directory = Path.GetDirectoryName(dstPath);
        if (directory?.Length > 0 && !Directory.Exists(directory))
            Directory.CreateDirectory(directory);

        using var srcStream = await ReadFileStreamAsync(srcPath);
        using var dstStream = System.IO.File.OpenWrite(dstPath); 
        srcStream.CopyTo(dstStream);

        return true;
    }

    public async Task<Stream> ReadFileStreamAsync(string path, long offset = 0, CancellationToken? token = null)
    {
        var (containerName, blobName) = GetContainerAndFileName(path);
        var blobContainerClient = GetContainerClient(containerName, blobName);

        var client = blobContainerClient.GetBlobClient(blobName);
        return await client.OpenReadAsync();
    }



    /// <summary>
    /// 
    /// </summary>
    /// <param name="containerName"></param>
    /// <param name="blobName"></param>
    /// <returns></returns>
    /// <exception cref="ApplicationException"></exception>
    private BlobContainerClient GetContainerClient(string containerName, string blobName)
    {
        if (string.IsNullOrEmpty(containerName)) throw new ApplicationException("File root is empty");
        if (string.IsNullOrEmpty(blobName)) throw new ApplicationException($"File name is empty");

        return BlobServiceClient.GetBlobContainerClient(containerName);
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="file"></param>
    /// <returns></returns>
    private static (string container, string fname) GetContainerAndFileName(File file)
    {
        return GetContainerAndFileName(file.Path);
    }

    private static (string container, string fname) GetContainerAndFileName(string? path)
    {
        var parts = path?.Split(Path.DirectorySeparatorChar, 2);
        return parts?.Length > 1 ? (parts[0], parts[1]) : (string.Empty, string.Empty);
    }
}
