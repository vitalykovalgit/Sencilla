namespace Sencilla.Component.Files.AmazonS3;

/// <summary>
/// Writable stream for <see cref="IFileStorage.OpenFileStream(File, long, CancellationToken)"/>.
/// S3 has no append and no open-for-write, so bytes are staged in a temp file and uploaded when
/// the stream is disposed — multipart internally, so object size is bounded by disk, not by RAM.
/// </summary>
/// <remarks>
/// Callers write with a plain <c>using</c> and read <see cref="Position"/> afterwards to record
/// the byte count, so <see cref="Dispose(bool)"/> must complete the upload synchronously.
/// Prefer <c>await using</c> where the call site allows it.
/// </remarks>
internal sealed class S3WriteStream : Stream
{
    private readonly IAmazonS3 _client;
    private readonly string _bucket;
    private readonly string _key;
    private readonly string _tempPath;
    private readonly FileStream _buffer;
    private readonly CancellationToken _token;
    private bool _flushed;

    public S3WriteStream(IAmazonS3 client, string bucket, string key, CancellationToken token = default)
    {
        _client = client;
        _bucket = bucket;
        _key = key;
        _token = token;
        _tempPath = Path.Combine(Path.GetTempPath(), $"s3w_{Guid.NewGuid():N}.tmp");
        _buffer = new FileStream(_tempPath, FileMode.CreateNew, FileAccess.ReadWrite, FileShare.None,
            81920, FileOptions.Asynchronous | FileOptions.SequentialScan);
    }

    public override bool CanRead => false;
    public override bool CanSeek => _buffer.CanSeek;
    public override bool CanWrite => true;
    public override long Length => _buffer.Length;

    public override long Position
    {
        get => _buffer.Position;
        set => _buffer.Position = value;
    }

    public override void Write(byte[] buffer, int offset, int count) => _buffer.Write(buffer, offset, count);
    public override void Write(ReadOnlySpan<byte> buffer) => _buffer.Write(buffer);

    public override Task WriteAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
        => _buffer.WriteAsync(buffer, offset, count, cancellationToken);

    public override ValueTask WriteAsync(ReadOnlyMemory<byte> buffer, CancellationToken cancellationToken = default)
        => _buffer.WriteAsync(buffer, cancellationToken);

    public override void Flush() => _buffer.Flush();
    public override long Seek(long offset, SeekOrigin origin) => _buffer.Seek(offset, origin);
    public override void SetLength(long value) => _buffer.SetLength(value);

    public override int Read(byte[] buffer, int offset, int count)
        => throw new NotSupportedException("S3WriteStream is write-only.");

    private async Task UploadAsync()
    {
        if (_flushed) return;
        _flushed = true;

        await _buffer.FlushAsync(_token);
        _buffer.Position = 0;

        // TransferUtility switches to multipart on its own for large payloads, so a
        // print-ready spread or an order archive is not held in memory.
        var transfer = new TransferUtility(_client);
        await transfer.UploadAsync(new TransferUtilityUploadRequest
        {
            BucketName = _bucket,
            Key = _key,
            InputStream = _buffer,
            AutoCloseStream = false,
        }, _token);
    }

    protected override void Dispose(bool disposing)
    {
        if (!disposing)
        {
            base.Dispose(disposing);
            return;
        }

        try
        {
            UploadAsync().GetAwaiter().GetResult();
        }
        finally
        {
            _buffer.Dispose();
            TryDeleteTemp();
            base.Dispose(disposing);
        }
    }

    public override async ValueTask DisposeAsync()
    {
        try
        {
            await UploadAsync();
        }
        finally
        {
            await _buffer.DisposeAsync();
            TryDeleteTemp();
            GC.SuppressFinalize(this);
        }
    }

    private void TryDeleteTemp()
    {
        try
        {
            if (System.IO.File.Exists(_tempPath))
                System.IO.File.Delete(_tempPath);
        }
        catch (IOException)
        {
            // A leftover temp file is noise, not a failure — never mask the upload result.
        }
    }
}
