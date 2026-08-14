namespace Sencilla.Component.Files.AmazonS3;

/// <summary>
/// S3 storage for AWS and for S3-compatible endpoints (Hetzner Object Storage, MinIO, R2).
/// </summary>
/// <remarks>
/// Two rules shape this implementation:
/// <list type="bullet">
/// <item>The bucket comes from <see cref="AmazonS3StorageOptions.Bucket"/> only. The stored path
/// is the object key (behind <see cref="AmazonS3StorageOptions.KeyPrefix"/>) — never a bucket
/// name, and buckets are never created implicitly.</item>
/// <item>Resumable writes map to multipart uploads whose part number is derived from the byte
/// offset, so a chunk re-sent after a dropped acknowledgement overwrites its own part instead of
/// duplicating data. The upload id is recovered from S3 itself, so nothing has to be persisted
/// between requests and any replica can continue an upload another replica started.</item>
/// </list>
/// </remarks>
public class AmazonS3Storage : IFileStorage
{
    private readonly IAmazonS3 Client;
    private readonly AmazonS3StorageOptions options;
    private readonly IFilePathResolver pathResolver;

    public AmazonS3Storage(AmazonS3StorageOptions options, IFilePathResolver pathResolver)
    {
        this.options = options;
        this.pathResolver = pathResolver;

        if (string.IsNullOrWhiteSpace(options.Bucket))
            throw new InvalidOperationException(
                $"{nameof(AmazonS3StorageOptions)}.{nameof(options.Bucket)} is required — S3 storage never " +
                "derives a bucket from the file path.");

        var config = new AmazonS3Config();

        if (!string.IsNullOrEmpty(options.ServiceUrl))
        {
            // ServiceURL and RegionEndpoint are mutually exclusive in the AWS SDK: whichever is
            // set last nulls the other. A non-AWS endpoint must set ServiceURL, and needs an
            // explicit signing region because there is no region to infer from the host.
            config.ServiceURL = options.ServiceUrl;
            config.ForcePathStyle = options.ForcePathStyle;
            config.AuthenticationRegion = string.IsNullOrEmpty(options.AuthenticationRegion)
                ? options.Region
                : options.AuthenticationRegion;
        }
        else if (!string.IsNullOrEmpty(options.Region))
        {
            // GetBySystemName never throws on an unknown name — it invents an AWS-partition
            // region and the request goes to a host that does not exist. Reject the mistake here
            // rather than at the first upload.
            var region = RegionEndpoint.GetBySystemName(options.Region);
            if (string.Equals(region.DisplayName, "Unknown", StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException(
                    $"'{options.Region}' is not an AWS region. For an S3-compatible endpoint set " +
                    $"{nameof(AmazonS3StorageOptions.ServiceUrl)} (e.g. https://fsn1.your-objectstorage.com) " +
                    $"and {nameof(AmazonS3StorageOptions.AuthenticationRegion)} instead.");

            config.RegionEndpoint = region;
        }
        else
        {
            throw new InvalidOperationException(
                $"Configure either {nameof(AmazonS3StorageOptions.Region)} (AWS) or " +
                $"{nameof(AmazonS3StorageOptions.ServiceUrl)} (S3-compatible endpoint).");
        }

        Client = !string.IsNullOrEmpty(options.AccessKey) && !string.IsNullOrEmpty(options.SecretKey)
            ? new AmazonS3Client(options.AccessKey, options.SecretKey, config)
            : new AmazonS3Client(config);
    }

    /// <summary>Test seam: an already-configured client (MinIO in a container, a stub).</summary>
    internal AmazonS3Storage(IAmazonS3 client, AmazonS3StorageOptions options, IFilePathResolver pathResolver)
    {
        Client = client;
        this.options = options;
        this.pathResolver = pathResolver;
    }

    public byte Type => options.Type;

    public string GetDirectory(string type, params object[] @params) => options.GetDirectory(type, @params);

    public string GetRootDirectory() => "/";

    private string Bucket => options.Bucket;

    private string Key(string? path) => S3Paths.ToKey(options.KeyPrefix, path);

    private string FolderPrefix(string? folder) => S3Paths.ToFolderPrefix(options.KeyPrefix, folder);

    // ── Directories (prefixes) ──────────────────────────────────────────────────────────────

    public async Task<string> RenameDirectoryAsync(string sourceDir, string destDir, CancellationToken token = default)
    {
        var srcPrefix = FolderPrefix(sourceDir);
        var dstPrefix = FolderPrefix(destDir);

        var keys = await ListKeysAsync(srcPrefix, token);

        foreach (var srcKey in keys)
        {
            var dstKey = dstPrefix + srcKey[srcPrefix.Length..];
            await Client.CopyObjectAsync(new CopyObjectRequest
            {
                SourceBucket = Bucket,
                SourceKey = srcKey,
                DestinationBucket = Bucket,
                DestinationKey = dstKey
            }, token);
        }

        // Delete only after every copy landed: a rename that fails halfway must leave the source
        // intact, because the caller treats the source as the archive of record.
        await DeleteKeysAsync(keys, token);

        return destDir;
    }

    /// <summary>
    /// S3 has no directories, but an empty directory is a signal callers rely on — the publish
    /// pipeline creates one to mark an order as mid-publish so the shop does not collect a
    /// half-written folder. Writing the conventional trailing-slash placeholder keeps that
    /// signal visible; listings skip it, and deleting the prefix removes it.
    /// </summary>
    public async Task CreateDirectoryAsync(string path)
    {
        var marker = FolderPrefix(path);
        if (marker.Length == 0)
            return;

        await Client.PutObjectAsync(new PutObjectRequest
        {
            BucketName = Bucket,
            Key = marker,
            ContentBody = string.Empty
        });
    }

    public async Task DeleteDirectoryAsync(string path)
    {
        var keys = await ListKeysAsync(FolderPrefix(path), CancellationToken.None);
        await DeleteKeysAsync(keys, CancellationToken.None);
    }

    public async Task<File[]> GetDirectoryEntriesAsync(string folder, CancellationToken token = default)
    {
        var prefix = FolderPrefix(folder);
        var entries = new List<File>();
        var request = new ListObjectsV2Request { BucketName = Bucket, Prefix = prefix };

        ListObjectsV2Response response;
        do
        {
            response = await Client.ListObjectsV2Async(request, token);

            foreach (var obj in response.S3Objects ?? [])
            {
                var relative = obj.Key[prefix.Length..];
                if (relative.Length == 0 || relative.EndsWith('/'))
                    continue;

                entries.Add(new File
                {
                    Id = Guid.Empty,
                    Size = obj.Size ?? 0,
                    // Leading slash, forward slashes, relative to the folder — the shape the
                    // local drive and Azure providers return, and what callers parse.
                    Path = "/" + relative,
                    Name = Path.GetFileName(relative)
                });
            }

            request.ContinuationToken = response.NextContinuationToken;
        }
        while (response.IsTruncated ?? false);

        return entries.ToArray();
    }

    private async Task<List<string>> ListKeysAsync(string prefix, CancellationToken token)
    {
        var keys = new List<string>();
        var request = new ListObjectsV2Request { BucketName = Bucket, Prefix = prefix };

        ListObjectsV2Response response;
        do
        {
            response = await Client.ListObjectsV2Async(request, token);
            foreach (var obj in response.S3Objects ?? [])
                keys.Add(obj.Key);

            request.ContinuationToken = response.NextContinuationToken;
        }
        while (response.IsTruncated ?? false);

        return keys;
    }

    private async Task DeleteKeysAsync(IReadOnlyList<string> keys, CancellationToken token)
    {
        // One request per 1000 keys instead of one per key: deleting a project folder object by
        // object is thousands of round trips.
        for (var i = 0; i < keys.Count; i += 1000)
        {
            var batch = keys.Skip(i).Take(1000).Select(k => new KeyVersion { Key = k }).ToList();
            await Client.DeleteObjectsAsync(new DeleteObjectsRequest { BucketName = Bucket, Objects = batch }, token);
        }
    }

    // ── Reads ───────────────────────────────────────────────────────────────────────────────

    public Task<Stream?> ReadFileAsync(File file, CancellationToken token = default)
        => ReadFileAsync(file.Path, token);

    public async Task<Stream?> ReadFileAsync(string? file, CancellationToken token = default)
    {
        try
        {
            // The live response stream, not a MemoryStream: this sits on every thumbnail request,
            // and buffering whole objects puts every concurrent original on the large object heap.
            var response = await Client.GetObjectAsync(new GetObjectRequest
            {
                BucketName = Bucket,
                Key = Key(file)
            }, token);

            return response.ResponseStream;
        }
        catch (AmazonS3Exception ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            // Missing object is null, not an exception — callers guard on null.
            return null;
        }
    }

    // ── Writes ──────────────────────────────────────────────────────────────────────────────

    public Stream OpenFileStream(File file, long offset = 0, CancellationToken token = default)
        => OpenFileStream(file.Path ?? pathResolver.GetFullPath(file), offset, token);

    public Stream OpenFileStream(string path, long offset = 0, CancellationToken token = default)
    {
        if (offset != 0)
            throw new NotSupportedException(
                "S3 objects cannot be opened for writing at an offset. Use WriteFileAsync, which " +
                "maps resumable chunks onto multipart parts.");

        return new S3WriteStream(Client, Bucket, Key(path), token);
    }

    public async Task<long> WriteFileAsync(File file, byte[] content, long offset = 0, CancellationToken token = default)
    {
        using var ms = new MemoryStream(content);
        return await WriteAsync(file, ms, offset, content.Length, token);
    }

    public Task<long> WriteFileAsync(File file, Stream stream, long offset = 0, long length = -1, CancellationToken token = default)
        => WriteAsync(file, stream, offset, length, token);

    private async Task<long> WriteAsync(File file, Stream stream, long offset, long length, CancellationToken token)
    {
        var key = Key(file.Path ?? pathResolver.GetFullPath(file));

        var lengthProvided = length >= 0;
        if (!lengthProvided && stream.CanSeek)
            length = stream.Length - stream.Position;

        if (!S3Paths.IsChunked(offset, length, file.Size, options.PartSize, lengthProvided))
            return await PutWholeAsync(key, stream, length, token);

        return await PutPartAsync(file, key, stream, offset, length, token);
    }

    /// <summary>Whole object in one call. TransferUtility switches to multipart internally past ~16 MB.</summary>
    private async Task<long> PutWholeAsync(string key, Stream stream, long length, CancellationToken token)
    {
        await using var payload = await Seekable(stream, token);

        var transfer = new TransferUtility(Client);
        await transfer.UploadAsync(new TransferUtilityUploadRequest
        {
            BucketName = Bucket,
            Key = key,
            InputStream = payload.Stream,
            AutoCloseStream = false,
        }, token);

        return payload.Length;
    }

    /// <summary>One resumable chunk = one multipart part, numbered from its offset.</summary>
    private async Task<long> PutPartAsync(File file, string key, Stream stream, long offset, long length, CancellationToken token)
    {
        await using var payload = await Seekable(stream, token);
        length = payload.Length;

        S3Paths.ValidateChunk(offset, length, file.Size, options.PartSize);

        var uploadId = await FindInflightAsync(key, token);
        if (uploadId == null)
        {
            // Nothing in flight at a non-zero offset means an earlier attempt already completed
            // this object and its acknowledgement was lost on the way back. Starting a fresh
            // upload id here would complete an object made of this chunk alone and overwrite the
            // finished one — silent truncation reported as success.
            if (offset > 0 && await AlreadyStoredAsync(key, offset + length, token))
                return offset + length;

            uploadId = await StartAsync(key, token);
        }

        var partNumber = S3Paths.PartNumber(offset, options.PartSize);

        // Re-uploading a part number replaces it, so a chunk resent after a dropped ack costs a
        // round trip and nothing else. This is the bug that corrupted Azure uploads.
        await Client.UploadPartAsync(new UploadPartRequest
        {
            BucketName = Bucket,
            Key = key,
            UploadId = uploadId,
            PartNumber = partNumber,
            PartSize = length,
            InputStream = payload.Stream,
        }, token);

        var newOffset = offset + length;

        if (S3Paths.IsLastChunk(offset, length, file.Size, options.PartSize))
            await CompleteAsync(key, uploadId, token);

        return newOffset;
    }

    /// <summary>
    /// The upload already in flight for this key, if any. S3 holds the state, so nothing has to
    /// be persisted between requests and a resumed upload can land on a different replica.
    /// </summary>
    private async Task<string?> FindInflightAsync(string key, CancellationToken token)
    {
        // Prefix search, so filter by exact key: "photo.jpg" is a prefix of "photo.jpg.bak".
        var request = new ListMultipartUploadsRequest { BucketName = Bucket, Prefix = key };
        MultipartUpload? newest = null;

        ListMultipartUploadsResponse response;
        do
        {
            response = await Client.ListMultipartUploadsAsync(request, token);

            foreach (var upload in response.MultipartUploads ?? [])
                if (upload.Key == key && (newest == null || upload.Initiated > newest.Initiated))
                    newest = upload;

            request.KeyMarker = response.NextKeyMarker;
            request.UploadIdMarker = response.NextUploadIdMarker;
        }
        while (response.IsTruncated ?? false);

        return newest?.UploadId;
    }

    private async Task<string> StartAsync(string key, CancellationToken token)
    {
        var started = await Client.InitiateMultipartUploadAsync(new InitiateMultipartUploadRequest
        {
            BucketName = Bucket,
            Key = key
        }, token);

        return started.UploadId;
    }

    /// <summary>Does a finished object at this key already hold at least <paramref name="atLeast"/> bytes?</summary>
    private async Task<bool> AlreadyStoredAsync(string key, long atLeast, CancellationToken token)
    {
        try
        {
            var meta = await Client.GetObjectMetadataAsync(new GetObjectMetadataRequest
            {
                BucketName = Bucket,
                Key = key
            }, token);

            return meta.ContentLength >= atLeast;
        }
        catch (AmazonS3Exception ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            return false;
        }
    }

    private async Task CompleteAsync(string key, string uploadId, CancellationToken token)
    {
        // Parts come from S3 rather than from anything we tracked, so the object is assembled
        // correctly even when earlier chunks were written by another process.
        var parts = new List<PartETag>();
        var request = new ListPartsRequest { BucketName = Bucket, Key = key, UploadId = uploadId };

        ListPartsResponse response;
        do
        {
            response = await Client.ListPartsAsync(request, token);
            foreach (var part in response.Parts ?? [])
                if (part.PartNumber is int number)
                    parts.Add(new PartETag(number, part.ETag));

            request.PartNumberMarker = response.NextPartNumberMarker?.ToString();
        }
        while (response.IsTruncated ?? false);

        await Client.CompleteMultipartUploadAsync(new CompleteMultipartUploadRequest
        {
            BucketName = Bucket,
            Key = key,
            UploadId = uploadId,
            PartETags = parts.OrderBy(p => p.PartNumber).ToList()
        }, token);
    }

    public async Task<bool> SaveFile(string file, Stream stream)
    {
        await PutWholeAsync(Key(file), stream, -1, CancellationToken.None);
        return true;
    }

    public Task<bool> SaveFile(string file, byte[] data) => SaveFile(file, new MemoryStream(data));

    // ── Deletes ─────────────────────────────────────────────────────────────────────────────

    public async Task<File?> DeleteFileAsync(File? file, CancellationToken token = default)
    {
        if (file == null || string.IsNullOrEmpty(file.Path))
            return null;

        var keys = new List<string> { Key(file.Path) };

        foreach (var resKey in file.Res?.Keys ?? [])
            keys.Add(Key(pathResolver.GetResolutionPath(file.Path, resKey, file.Res![resKey]?.Ct)));

        try
        {
            await DeleteKeysAsync(keys, token);
            return file;
        }
        catch (AmazonS3Exception ex)
        {
            throw new IOException(
                $"S3 error when deleting object: {file.Path}. Status: {ex.StatusCode}, Error: {ex.ErrorCode}", ex);
        }
    }

    public async Task DeleteFileAsync(string filePath, CancellationToken token = default)
    {
        if (string.IsNullOrWhiteSpace(filePath))
            return;

        try
        {
            await Client.DeleteObjectAsync(new DeleteObjectRequest { BucketName = Bucket, Key = Key(filePath) }, token);
        }
        catch (AmazonS3Exception ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            // Already gone.
        }
        catch (AmazonS3Exception ex)
        {
            throw new IOException(
                $"S3 error when deleting object: {filePath}. Status: {ex.StatusCode}, Error: {ex.ErrorCode}", ex);
        }
    }

    // ── Archives ────────────────────────────────────────────────────────────────────────────

    public async Task ZipFolderAsync(string folderToArchive, string destinationFile, CancellationToken token = default)
    {
        var prefix = FolderPrefix(folderToArchive);
        var tempZipPath = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid():N}.zip");

        try
        {
            using (var fs = new FileStream(tempZipPath, FileMode.Create, FileAccess.Write, FileShare.None, 81920, FileOptions.Asynchronous))
            using (var archive = new ZipArchive(fs, ZipArchiveMode.Create))
            {
                foreach (var key in await ListKeysAsync(prefix, token))
                {
                    var entryName = key[prefix.Length..];
                    if (entryName.Length == 0 || entryName.EndsWith('/'))
                        continue;

                    var response = await Client.GetObjectAsync(new GetObjectRequest { BucketName = Bucket, Key = key }, token);
                    await using var source = response.ResponseStream;

                    var entry = archive.CreateEntry(entryName, CompressionLevel.Optimal);
                    await using var entryStream = entry.Open();
                    await source.CopyToAsync(entryStream, token);
                }
            }

            await using var upload = new FileStream(tempZipPath, FileMode.Open, FileAccess.Read, FileShare.Read, 81920, FileOptions.Asynchronous | FileOptions.SequentialScan);
            await PutWholeAsync(Key(destinationFile), upload, upload.Length, token);
        }
        finally
        {
            if (System.IO.File.Exists(tempZipPath))
                System.IO.File.Delete(tempZipPath);
        }
    }

    public async Task AddFilesToZipAsync(string zipFilePath, IEnumerable<string> filesToAdd, string prefixToStrip = "/published", CancellationToken token = default)
    {
        var zipKey = Key(zipFilePath);
        var tempZipPath = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid():N}.zip");

        try
        {
            await using (var existing = await ReadFileAsync(zipFilePath, token))
            {
                if (existing == null)
                    throw new FileNotFoundException($"Zip file not found: {zipFilePath}");

                await using var fs = new FileStream(tempZipPath, FileMode.Create, FileAccess.Write, FileShare.None, 81920, FileOptions.Asynchronous);
                await existing.CopyToAsync(fs, token);
            }

            using (var fs = new FileStream(tempZipPath, FileMode.Open, FileAccess.ReadWrite, FileShare.None, 81920, FileOptions.Asynchronous))
            using (var archive = new ZipArchive(fs, ZipArchiveMode.Update))
            {
                foreach (var filePath in filesToAdd)
                {
                    await using var source = await ReadFileAsync(filePath, token);
                    if (source == null)
                        continue;

                    var entryName = filePath;
                    if (!string.IsNullOrEmpty(prefixToStrip) && entryName.StartsWith(prefixToStrip, StringComparison.OrdinalIgnoreCase))
                        entryName = entryName[prefixToStrip.Length..].TrimStart('/', '\\');

                    entryName = entryName.Replace('\\', '/');

                    archive.GetEntry(entryName)?.Delete();

                    var entry = archive.CreateEntry(entryName, CompressionLevel.Optimal);
                    await using var entryStream = entry.Open();
                    await source.CopyToAsync(entryStream, token);
                }
            }

            await using var upload = new FileStream(tempZipPath, FileMode.Open, FileAccess.Read, FileShare.Read, 81920, FileOptions.Asynchronous | FileOptions.SequentialScan);
            await PutWholeAsync(zipKey, upload, upload.Length, token);
        }
        finally
        {
            if (System.IO.File.Exists(tempZipPath))
                System.IO.File.Delete(tempZipPath);
        }
    }

    // ── Materialise to the local filesystem ─────────────────────────────────────────────────

    public Task<bool> CopyToFileAsync(File srcFile, string dstPath) => CopyToFileAsync(srcFile.Path, dstPath);

    /// <summary>
    /// Copies an object out of storage onto a LOCAL path. Callers hand a temp file to native
    /// code (OpenCV, the prepress renderer) that cannot read a stream, then stat the result — so
    /// the destination is a filesystem path, not another key.
    /// </summary>
    public async Task<bool> CopyToFileAsync(string? srcPath, string dstPath)
    {
        await using var source = await ReadFileAsync(srcPath);
        if (source == null)
            return false;

        var folder = Path.GetDirectoryName(dstPath);
        if (!string.IsNullOrEmpty(folder) && !Directory.Exists(folder))
            Directory.CreateDirectory(folder);

        await using var destination = new FileStream(dstPath, FileMode.Create, FileAccess.Write, FileShare.None,
            81920, FileOptions.Asynchronous | FileOptions.SequentialScan);
        await source.CopyToAsync(destination);
        return true;
    }

    // ── Helpers ─────────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// S3 needs a length up front. Request bodies are not seekable, so stage them — on disk, not
    /// in memory: one part is 8 MiB but a whole-object write can be gigabytes.
    /// </summary>
    private static async Task<StagedPayload> Seekable(Stream stream, CancellationToken token)
    {
        if (stream.CanSeek)
            return new StagedPayload(stream, stream.Length - stream.Position, null);

        var path = Path.Combine(Path.GetTempPath(), $"s3s_{Guid.NewGuid():N}.tmp");
        var buffer = new FileStream(path, FileMode.CreateNew, FileAccess.ReadWrite, FileShare.None,
            81920, FileOptions.Asynchronous | FileOptions.SequentialScan);

        try
        {
            await stream.CopyToAsync(buffer, token);
            await buffer.FlushAsync(token);
            buffer.Position = 0;
        }
        catch
        {
            // A cancelled upload must not leave the staging file (and its handle) behind — this
            // runs on every chunk of every upload.
            await buffer.DisposeAsync();
            TryDelete(path);
            throw;
        }

        return new StagedPayload(buffer, buffer.Length, path);
    }

    private static void TryDelete(string path)
    {
        try
        {
            if (System.IO.File.Exists(path))
                System.IO.File.Delete(path);
        }
        catch (IOException)
        {
            // Leftover temp file is noise, not a failure.
        }
    }

    private sealed class StagedPayload(Stream stream, long length, string? tempPath) : IAsyncDisposable
    {
        public Stream Stream { get; } = stream;
        public long Length { get; } = length;

        public async ValueTask DisposeAsync()
        {
            // Only the staging copy is ours to close — the caller still owns the stream it passed.
            if (tempPath == null)
                return;

            await Stream.DisposeAsync();
            TryDelete(tempPath);
        }
    }
}
