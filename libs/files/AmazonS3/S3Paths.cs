namespace Sencilla.Component.Files.AmazonS3;

/// <summary>
/// Key mapping and chunk arithmetic for <see cref="AmazonS3Storage"/>. Pure functions, kept
/// separate so the rules that decide where bytes land are testable without an S3 endpoint.
/// </summary>
public static class S3Paths
{
    /// <summary>S3's minimum size for every multipart part except the last.</summary>
    public const int MinPartSize = 5 * 1024 * 1024;

    /// <summary>S3's hard limit on the number of parts in one multipart upload.</summary>
    public const int MaxParts = 10_000;

    /// <summary>
    /// Stored path to object key: forward slashes, no leading slash, prefixed. The whole path
    /// becomes the key — the bucket comes from configuration, never from the path.
    /// </summary>
    public static string ToKey(string? keyPrefix, string? path)
    {
        var normalized = (path ?? string.Empty).Replace('\\', '/').TrimStart('/');
        var prefix = (keyPrefix ?? string.Empty).Replace('\\', '/').TrimStart('/');

        if (prefix.Length > 0 && !prefix.EndsWith('/'))
            prefix += '/';

        return prefix + normalized;
    }

    /// <summary>Key prefix for listing a folder — <see cref="ToKey"/> plus a trailing slash.</summary>
    public static string ToFolderPrefix(string? keyPrefix, string? folder)
    {
        var key = ToKey(keyPrefix, folder);
        if (key.Length > 0 && !key.EndsWith('/'))
            key += '/';
        return key;
    }

    /// <summary>
    /// Part number for a chunk starting at <paramref name="offset"/>. Derived from the offset
    /// rather than counted, so a chunk re-sent after a dropped acknowledgement overwrites its
    /// own part instead of appending a duplicate.
    /// </summary>
    public static int PartNumber(long offset, int partSize)
        => checked((int)(offset / partSize) + 1);

    /// <summary>
    /// Is this the chunk that completes the object? A declared size answers it outright.
    /// Without one — resolution uploads carry <c>Size = 0</c> — the answer comes from the part
    /// rule instead: every chunk but the last is exactly <paramref name="partSize"/>, so a short
    /// chunk is the end. Guessing wrong here leaves the upload uncompleted and the object absent.
    /// </summary>
    public static bool IsLastChunk(long offset, long length, long size, int partSize)
        => size > 0 ? offset + length >= size : length < partSize;

    /// <summary>
    /// Would this write need multipart? Only a resumable chunk does, and a resumable chunk is
    /// recognisable: it either continues an object (<paramref name="offset"/> above zero) or it
    /// is a first chunk carrying exactly one part of something larger.
    /// </summary>
    /// <param name="lengthProvided">
    /// The caller passed an explicit length. Chunked uploads always do; a whole-object write
    /// leaves it at -1. Without this, a server-side write that happens to be exactly one part
    /// long would be mistaken for a first chunk and never completed.
    /// </param>
    /// <remarks>
    /// Deliberately NOT "the payload is shorter than File.Size": the zero-byte placeholder
    /// written at file creation, and any overwrite smaller than the recorded size (a sanitised
    /// SVG, a re-compressed derivative), are whole objects and must go up in one request.
    /// </remarks>
    public static bool IsChunked(long offset, long length, long size, int partSize, bool lengthProvided)
        => offset > 0 || (lengthProvided && length == partSize && (size <= 0 || size > length));

    /// <summary>
    /// Fail loudly on a chunk layout S3 cannot complete, naming the fix. Without this the
    /// upload succeeds part by part and only dies at CompleteMultipartUpload with
    /// <c>EntityTooSmall</c>, long after the client has sent every byte.
    /// </summary>
    public static void ValidateChunk(long offset, long length, long size, int partSize)
    {
        if (partSize < MinPartSize)
            throw new InvalidOperationException(
                $"PartSize is {partSize} bytes; S3 requires at least {MinPartSize} (5 MiB). " +
                "Raise AmazonS3StorageOptions.PartSize and the client chunk size together.");

        if (offset % partSize != 0)
            throw new InvalidOperationException(
                $"Chunk offset {offset} is not a multiple of PartSize {partSize}. The client chunk " +
                "size must equal PartSize so part numbers can be derived from the offset.");

        if (PartNumber(offset, partSize) > MaxParts)
            throw new InvalidOperationException(
                $"Offset {offset} is part {PartNumber(offset, partSize)}, past S3's limit of {MaxParts} parts. " +
                $"With PartSize {partSize} the largest object is {(long)partSize * MaxParts} bytes.");

        if (!IsLastChunk(offset, length, size, partSize) && length < partSize)
            throw new InvalidOperationException(
                $"Chunk at offset {offset} is {length} bytes but is not the final chunk of a {size}-byte " +
                $"object. S3 rejects a multipart upload whose non-final parts are under {MinPartSize} bytes; " +
                $"the client must send {partSize}-byte chunks.");
    }
}
