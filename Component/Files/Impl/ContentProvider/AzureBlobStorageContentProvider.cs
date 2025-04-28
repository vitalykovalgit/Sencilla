namespace Sencilla.Component.Files;

public class AzureBlobStorageContentProvider : IFileContentProvider
{
    public const FileContentProviderType ProviderType = FileContentProviderType.AzureBlobStorage;
    FileContentProviderType IFileContentProvider.ProviderType => ProviderType;

    private readonly BlobServiceClient _blobServiceClient;

    public AzureBlobStorageContentProvider(IConfigProvider<FileContentProviderOptions> configProvider)
    {
        var config = configProvider.GetConfig();
        _blobServiceClient = new BlobServiceClient(config.ConnectionString);
    }

    public Task<File> DeleteFileAsync(File file, CancellationToken? token = null)
    {
        throw new NotImplementedException();
    }

    private BlobContainerClient GetContainerClient(string containerName, string blobName)
    {
        if (string.IsNullOrEmpty(containerName)) throw new ApplicationException("File root is empty");

        if (string.IsNullOrEmpty(blobName)) throw new ApplicationException($"File name is empty");

        var blobContainerClient = _blobServiceClient.GetBlobContainerClient(containerName);

        return blobContainerClient;
    }

    public Stream OpenFileStream(File file, long offset = 0, CancellationToken? token = null)
    {
        var (containerName, blobName) = GetContainerAndFileName(file);

        var blobContainerClient = GetContainerClient(containerName, blobName);

        var blobClient = blobContainerClient.GetBlobClient(blobName);

        return blobClient.OpenWrite(true);
    }

    public Stream ReadFile(File file, CancellationToken? token = null) => ReadFileAsync(file, token).Result;

    public async Task<Stream> ReadFileAsync(File file, CancellationToken? token = null)
    {
        var (containerName, blobName) = GetContainerAndFileName(file);

        var blobContainerClient = GetContainerClient(containerName, blobName);

        var blobClient = blobContainerClient.GetBlobClient(blobName);

        var downloadResponse = await blobClient.DownloadStreamingAsync();

        // System.NotSupportedException: Specified method is not supported.
        //  at Azure.Core.Pipeline.RetriableStream.RetriableStreamImpl.get_Length()
        //  at OpenCvSharp.Mat.FromStream(Stream stream, ImreadModes mode)
        //return downloadResponse.Value.Content;

        var ms = new MemoryStream();
        await downloadResponse.Value.Content.CopyToAsync(ms);
        ms.Position = 0;

        return ms;
    }

    public async Task<long> WriteFileAsync(File file, byte[] content, long offset = 0, CancellationToken? token = null)
    {
        using var ms = new MemoryStream(content);
        return await WriteStreamToBlobAsync(file, ms, offset, -1, token);
    }

    public Task<long> WriteFileAsync(File file, Stream stream, long offset = 0, long length = -1, CancellationToken? token = null) =>
        WriteStreamToBlobAsync(file, stream, offset, length, token);

    private async Task<long> WriteStreamToBlobAsync(File file, Stream stream, long offset = 0, long length = -1, CancellationToken? token = null)
    {
        var (containerName, blobName) = GetContainerAndFileName(file);

        var blobContainerClient = GetContainerClient(containerName, blobName);

        await blobContainerClient.CreateIfNotExistsAsync();

        var appendBlobClient = blobContainerClient.GetAppendBlobClient(blobName);
        await appendBlobClient.CreateIfNotExistsAsync();

        if (stream.Length == 0)
            return 0;

        using var ms = new MemoryStream();
        if (!stream.CanSeek)
        {
            await stream.CopyToAsync(ms);
            ms.Position = 0;
            stream = ms;
        }

        int maxBlockSize = appendBlobClient.AppendBlobMaxAppendBlockBytes;
        long commitedOffset = 0, lastBlockSize = 0, bytesLeft = stream.Length;
        byte[] buffer = new byte[maxBlockSize];

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

    private static (string container, string fname) GetContainerAndFileName(File file)
    {
        var parts = file.FullName?.Split(Path.DirectorySeparatorChar, 2);

        return parts?.Length > 1 ? (parts[0], parts[1]) : (string.Empty, string.Empty);
    }
}
