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

    public async Task<string> RenameDirectoryAsync(string sourceDir, string destDir, CancellationToken token = default)
    {
        var (srcBucket, srcPrefix) = GetBucketAndKey(sourceDir);
        var (dstBucket, dstPrefix) = GetBucketAndKey(destDir);

        if (!srcPrefix.EndsWith('/'))
            srcPrefix += '/';
        if (!dstPrefix.EndsWith('/'))
            dstPrefix += '/';

        // List all objects with the source prefix
        var listRequest = new ListObjectsV2Request
        {
            BucketName = srcBucket,
            Prefix = srcPrefix
        };

        ListObjectsV2Response listResponse;
        var objectsToMove = new List<string>();

        do
        {
            listResponse = await Client.ListObjectsV2Async(listRequest, token);
            objectsToMove.AddRange(listResponse.S3Objects.Select(o => o.Key));
            listRequest.ContinuationToken = listResponse.NextContinuationToken;
        } 
        while (listResponse.IsTruncated ?? false);

        // Copy and delete each object
        foreach (var srcKey in objectsToMove)
        {
            var relativePath = srcKey.Substring(srcPrefix.Length);
            var dstKey = dstPrefix + relativePath;

            // Copy object
            var copyRequest = new CopyObjectRequest
            {
                SourceBucket = srcBucket,
                SourceKey = srcKey,
                DestinationBucket = dstBucket,
                DestinationKey = dstKey
            };
            await Client.CopyObjectAsync(copyRequest, token);

            // Delete source object
            var deleteRequest = new DeleteObjectRequest
            {
                BucketName = srcBucket,
                Key = srcKey
            };
            await Client.DeleteObjectAsync(deleteRequest, token);
        }

        return destDir;
    }

    public async Task CreateDirectoryAsync(string path)
    {
        // S3 doesn't have real directories, but we can create a marker object
        // or ensure the bucket exists if path is just a bucket name
        var (bucket, key) = GetBucketAndKey(path);

        if (string.IsNullOrEmpty(bucket))
            return;

        // Ensure bucket exists
        try
        {
            await Client.HeadBucketAsync(new HeadBucketRequest { BucketName = bucket });
        }
        catch (AmazonS3Exception ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            await Client.PutBucketAsync(new PutBucketRequest { BucketName = bucket });
        }

        // If there's a key (subdirectory), create a marker object to represent the directory
        if (!string.IsNullOrEmpty(key))
        {
            var markerKey = key.TrimEnd('/') + '/';

            // Check if marker already exists
            try
            {
                await Client.GetObjectMetadataAsync(bucket, markerKey);
            }
            catch (AmazonS3Exception ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                // Create directory marker
                var putRequest = new PutObjectRequest
                {
                    BucketName = bucket,
                    Key = markerKey,
                    ContentBody = string.Empty
                };
                await Client.PutObjectAsync(putRequest);
            }
        }
    }

    public async Task DeleteDirectoryAsync(string path)
    {
        var (bucket, prefix) = GetBucketAndKey(path);

        if (string.IsNullOrEmpty(bucket))
            return;

        // If no prefix, delete the entire bucket
        if (string.IsNullOrEmpty(prefix))
        {
            try
            {
                await Client.DeleteBucketAsync(bucket);
            }
            catch (AmazonS3Exception)
            {
                // Bucket might not exist or might not be empty, ignore
            }
            return;
        }

        // Delete all objects with the specified prefix
        if (!prefix.EndsWith('/'))
            prefix += '/';

        var listRequest = new ListObjectsV2Request
        {
            BucketName = bucket,
            Prefix = prefix
        };

        ListObjectsV2Response listResponse;
        do
        {
            listResponse = await Client.ListObjectsV2Async(listRequest);

            foreach (var obj in listResponse.S3Objects)
            {
                var deleteRequest = new DeleteObjectRequest
                {
                    BucketName = bucket,
                    Key = obj.Key
                };
                await Client.DeleteObjectAsync(deleteRequest);
            }

            listRequest.ContinuationToken = listResponse.NextContinuationToken;
        } while (listResponse.IsTruncated ?? false);
    }

    public Task<File[]> GetDirectoryEntriesAsync(string folder, CancellationToken token = default)
    {
        throw new NotImplementedException();
    }

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
        if (file == null)
            return null;

        try
        {
            var (bucket, key) = GetBucketAndKey(file.Path);

            if (string.IsNullOrEmpty(bucket) || string.IsNullOrEmpty(key))
                return null;

            var deleteRequest = new DeleteObjectRequest 
            { 
                BucketName = bucket, 
                Key = key 
            };

            var response = await Client.DeleteObjectAsync(deleteRequest, token);

            // Check if deletion was successful (HTTP 204 No Content)
            if (response.HttpStatusCode == System.Net.HttpStatusCode.NoContent)
                return file;

            return null;
        }
        catch (AmazonS3Exception ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            // File doesn't exist, return null
            return null;
        }
        catch (AmazonS3Exception ex)
        {
            throw new IOException($"S3 error when deleting object: {file.Path}. Status: {ex.StatusCode}, Error: {ex.ErrorCode}", ex);
        }
    }

    public async Task DeleteFileAsync(string filePath, CancellationToken token = default)
    {
        if (string.IsNullOrWhiteSpace(filePath))
            return;

        try
        {
            var (bucket, key) = GetBucketAndKey(filePath);

            if (string.IsNullOrEmpty(bucket) || string.IsNullOrEmpty(key))
                return;

            var deleteRequest = new DeleteObjectRequest 
            { 
                BucketName = bucket, 
                Key = key 
            };

            var response = await Client.DeleteObjectAsync(deleteRequest, token);
            return;
        }
        catch (AmazonS3Exception ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            // File doesn't exist, return null
            return;
        }
        catch (AmazonS3Exception ex)
        {
            throw new IOException($"S3 error when deleting object: {filePath}. Status: {ex.StatusCode}, Error: {ex.ErrorCode}", ex);
        }
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

    public Task AddFilesToZipAsync(string zipFilePath, IEnumerable<string> filesToAdd, string prefixToStrip = "/published", CancellationToken token = default)
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
