namespace Sencilla.Component.Files.AmazonS3;

public class AmazonS3Storage : IFileStorage
{
    private readonly IAmazonS3 Client;
    private readonly AmazonS3StorageOptions options;

    public AmazonS3Storage(AmazonS3StorageOptions options)
    {
        this.options = options;
        var config = new AmazonS3Config();
        if (!string.IsNullOrEmpty(options.Region))
            config.RegionEndpoint = RegionEndpoint.GetBySystemName(options.Region);

        if (!string.IsNullOrEmpty(options.AccessKey) && !string.IsNullOrEmpty(options.SecretKey))
            Client = new AmazonS3Client(options.AccessKey, options.SecretKey, config);
        else
            Client = new AmazonS3Client(config);
    }

    public byte Type => options.Type;

    public string GetDirectory(string type, params object[] @params) => options.GetDirectory(type, @params);

    public string GetRootDirectory() => "/";

    public Stream OpenFileStream(File file, long offset = 0, CancellationToken token = default)
    {
        throw new NotImplementedException("Streaming write is not implemented for S3 client in this simplified implementation");
    }

    public Stream OpenFileStream(string path, long offset = 0, CancellationToken token = default)
    {
        throw new NotImplementedException("Streaming write is not implemented for S3 client in this simplified implementation");
    }

    public async Task<Stream?> ReadFileAsync(File file, CancellationToken token = default)
    {
        var (bucket, key) = GetBucketAndKey(file.Path);
        var response = await Client.GetObjectAsync(new GetObjectRequest { BucketName = bucket, Key = key });
        var ms = new MemoryStream();
        await response.ResponseStream.CopyToAsync(ms);
        ms.Position = 0;
        return ms;
    }

    public async Task<Stream?> ReadFileAsync(string file, CancellationToken token = default)
    {
        throw new NotImplementedException("Reading by path is not implemented for S3 client in this simplified implementation");
    }


    public async Task<long> WriteFileAsync(File file, byte[] content, long offset = 0, CancellationToken token = default)
    {
        using var ms = new MemoryStream(content);
        return await WriteStreamToS3Async(file, ms, token);
    }

    public Task<long> WriteFileAsync(File file, Stream stream, long offset = 0, long length = -1, CancellationToken token = default)
        => WriteStreamToS3Async(file, stream, token);

    public async Task<File?> DeleteFileAsync(File? file, CancellationToken token = default)
    {
        if (file == null) return null;
        var (bucket, key) = GetBucketAndKey(file.Path);
        var req = new DeleteObjectRequest { BucketName = bucket, Key = key };
        await Client.DeleteObjectAsync(req, token);
        return file;
    }

    private async Task<long> WriteStreamToS3Async(File file, Stream stream, CancellationToken token = default)
    {
        var (bucket, key) = GetBucketAndKey(file.Path);

        // ensure bucket exists
        try
        {
            await Client.HeadBucketAsync(new HeadBucketRequest { BucketName = bucket }, token);
        }
        catch (AmazonS3Exception ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            await Client.PutBucketAsync(new PutBucketRequest { BucketName = bucket }, token);
        }
        catch
        {
            // Ignore other errors (e.g., access denied) and try to put object anyway
        }

        var putReq = new PutObjectRequest
        {
            BucketName = bucket,
            Key = key,
            InputStream = stream
        };

        var resp = await Client.PutObjectAsync(putReq, token);
        return stream.CanSeek ? stream.Length : 0;
    }

    public Task ZipFolderAsync(string folderToArchive, string destinationFile, CancellationToken token = default)
    {
        throw new NotImplementedException();
    }

    public async Task<bool> SaveFile(string file, Stream stream)
    {
        var f = new File { Path = file };
        return await WriteStreamToS3Async(f, stream) > 0;
    }

    public Task<bool> SaveFile(string file, byte[] data)
    {
        return SaveFile(file, new MemoryStream(data));
    }

    public Task<bool> CopyToFileAsync(File srcFile, string dstPath)
    {
        return CopyToFileAsync(srcFile.Path, dstPath);
    }

    public async Task<bool> CopyToFileAsync(string srcPath, string dstPath)
    {
        using var src = await ReadFileAsync(new File { Path = srcPath });
        if (src == null) return false;
        using var fs = System.IO.File.OpenWrite(dstPath);
        await src.CopyToAsync(fs);
        return true;
    }

    private static (string bucket, string key) GetBucketAndKey(string? path)
    {
        if (string.IsNullOrEmpty(path)) return (string.Empty, string.Empty);
        var parts = path.Split(new[] { Path.DirectorySeparatorChar, '/' }, 2);
        return parts.Length > 1 ? (parts[0], parts[1]) : (parts[0], string.Empty);
    }
}
