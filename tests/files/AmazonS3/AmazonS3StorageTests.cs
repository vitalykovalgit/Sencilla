namespace Sencilla.Component.Files.AmazonS3.Tests;

/// <summary>
/// Live integration tests against a real S3 endpoint — Hetzner Object Storage, or MinIO in a
/// container. Objects are written under a throwaway prefix and deleted on dispose.
///
/// Set SENCILLA_TEST_S3_BUCKET plus credentials to run; when unset the tests no-op, so the suite
/// stays green on a machine with no S3 access. Mirrors the Azure suite's gating.
///
///   SENCILLA_TEST_S3_BUCKET      required — an existing bucket, nothing is auto-created
///   SENCILLA_TEST_S3_ACCESS_KEY  required
///   SENCILLA_TEST_S3_SECRET_KEY  required
///   SENCILLA_TEST_S3_ENDPOINT    e.g. https://fsn1.your-objectstorage.com (omit for AWS)
///   SENCILLA_TEST_S3_REGION      AWS region, or the Hetzner location used for signing
///   SENCILLA_TEST_S3_PATH_STYLE  "true" for MinIO
///
/// Covers the failure modes that cost real data: a chunk resent after a dropped acknowledgement,
/// a resumed upload continuing from S3's own state, and a short final part.
/// </summary>
public class AmazonS3StorageTests : IAsyncLifetime
{
    private const int MB = 1024 * 1024;
    private const int PartSize = 5 * MB;   // the S3 minimum, so tests move as few bytes as possible

    private readonly string? _bucket = Environment.GetEnvironmentVariable("SENCILLA_TEST_S3_BUCKET");
    private readonly string _prefix = $"sencilla-tests/{Guid.NewGuid():N}/";
    private AmazonS3Storage? _storage;

    private bool Enabled => !string.IsNullOrEmpty(_bucket);

    public Task InitializeAsync()
    {
        if (Enabled)
            _storage = new AmazonS3Storage(Options(), new Mock<IFilePathResolver>().Object);

        return Task.CompletedTask;
    }

    public async Task DisposeAsync()
    {
        if (_storage != null)
            await _storage.DeleteDirectoryAsync("");
    }

    private AmazonS3StorageOptions Options() => new()
    {
        Bucket = _bucket!,
        KeyPrefix = _prefix,
        PartSize = PartSize,
        AccessKey = Environment.GetEnvironmentVariable("SENCILLA_TEST_S3_ACCESS_KEY"),
        SecretKey = Environment.GetEnvironmentVariable("SENCILLA_TEST_S3_SECRET_KEY"),
        ServiceUrl = Environment.GetEnvironmentVariable("SENCILLA_TEST_S3_ENDPOINT"),
        Region = Environment.GetEnvironmentVariable("SENCILLA_TEST_S3_REGION"),
        AuthenticationRegion = Environment.GetEnvironmentVariable("SENCILLA_TEST_S3_REGION"),
        ForcePathStyle = Environment.GetEnvironmentVariable("SENCILLA_TEST_S3_PATH_STYLE") == "true",
    };

    private static byte[] Filled(byte value, int size)
    {
        var bytes = new byte[size];
        Array.Fill(bytes, value);
        return bytes;
    }

    private static File NewFile(long size) => new()
    {
        Id = Guid.NewGuid(),
        Name = "orig.bin",
        Size = size,
        Path = $"user{Guid.NewGuid():N}/project1/orig.bin",
    };

    private async Task<byte[]> ReadAll(File file)
    {
        await using var stream = await _storage!.ReadFileAsync(file);
        using var ms = new MemoryStream();
        await stream!.CopyToAsync(ms);
        return ms.ToArray();
    }

    private Task<long> WriteChunk(File file, byte[] data, long offset)
    {
        var ms = new MemoryStream(data);
        return _storage!.WriteFileAsync(file, ms, offset, data.Length);
    }

    [Fact]
    public async Task SmallFile_GoesUpInOneRequest_AndComesBackIntact()
    {
        if (!Enabled) return;

        var payload = Filled(0x11, 64 * 1024);
        var file = NewFile(payload.Length);

        var offset = await WriteChunk(file, payload, 0);

        Assert.Equal(payload.Length, offset);
        Assert.Equal(payload, await ReadAll(file));
    }

    [Fact]
    public async Task ChunkedUpload_AssemblesEveryPartInOrder()
    {
        if (!Enabled) return;

        var first = Filled(0xA1, PartSize);
        var last = Filled(0xB2, 1024);
        var file = NewFile(first.Length + last.Length);

        var offset = await WriteChunk(file, first, 0);
        offset = await WriteChunk(file, last, offset);

        Assert.Equal(file.Size, offset);

        var stored = await ReadAll(file);
        Assert.Equal(file.Size, stored.Length);
        Assert.Equal(first, stored[..PartSize]);
        Assert.Equal(last, stored[PartSize..]);
    }

    [Fact]
    public async Task ResentChunk_IsNotStoredTwice()
    {
        if (!Enabled) return;

        // The dropped-ack retry: S3 committed the part, the response never reached the client, so
        // the client re-sends from its last known offset. The part number comes from the offset,
        // so this must overwrite — the Azure provider appended and corrupted the blob.
        var first = Filled(0xA1, PartSize);
        var last = Filled(0xB2, 1024);
        var file = NewFile(first.Length + last.Length);

        await WriteChunk(file, first, 0);
        var offset = await WriteChunk(file, first, 0);   // same chunk, same offset, again
        offset = await WriteChunk(file, last, offset);

        var stored = await ReadAll(file);

        Assert.Equal(file.Size, stored.Length);
        Assert.Equal(first, stored[..PartSize]);
        Assert.Equal(last, stored[PartSize..]);
    }

    [Fact]
    public async Task ResumedUpload_ContinuesOnAFreshStorageInstance()
    {
        if (!Enabled) return;

        // Upload state lives in S3, not in the process, so a chunk may land on any replica.
        var first = Filled(0xC3, PartSize);
        var last = Filled(0xD4, 2048);
        var file = NewFile(first.Length + last.Length);

        await WriteChunk(file, first, 0);

        var replica = new AmazonS3Storage(Options(), new Mock<IFilePathResolver>().Object);
        await using var tail = new MemoryStream(last);
        var offset = await replica.WriteFileAsync(file, tail, PartSize, last.Length);

        Assert.Equal(file.Size, offset);
        Assert.Equal(file.Size, (await ReadAll(file)).Length);
    }

    [Fact]
    public async Task UndersizedChunk_FailsImmediately_NotAtCompletion()
    {
        if (!Enabled) return;

        var file = NewFile(100 * MB);
        await using var chunk = new MemoryStream(Filled(0xE5, 4 * MB));

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => _storage!.WriteFileAsync(file, chunk, 0, 4 * MB));
    }

    [Fact]
    public async Task ZeroBytePlaceholder_IsWritten_NotRejected()
    {
        if (!Enabled) return;

        // File creation writes an empty object while File.Size already carries the declared upload
        // length. If this throws, every POST /api/v1/files 500s and no upload can start.
        var file = NewFile(3_000_000);

        await _storage!.WriteFileAsync(file, []);

        Assert.Empty(await ReadAll(file));
    }

    [Fact]
    public async Task ResolutionUpload_WithNoDeclaredSize_StillCompletes()
    {
        if (!Enabled) return;

        // Resolution files are built as `new File { Path = …, Storage = … }` — Size stays 0, so
        // completion has to come from the short final chunk.
        var first = Filled(0x31, PartSize);
        var last = Filled(0x32, 4096);
        var file = new File { Id = Guid.NewGuid(), Name = "res.bin", Size = 0, Path = $"user{Guid.NewGuid():N}/res_600.bin" };

        var offset = await WriteChunk(file, first, 0);
        offset = await WriteChunk(file, last, offset);

        Assert.Equal(first.Length + last.Length, offset);
        Assert.Equal(first.Length + last.Length, (await ReadAll(file)).Length);
    }

    [Fact]
    public async Task FinalChunkResentAfterCompletion_LeavesTheFinishedObjectAlone()
    {
        if (!Enabled) return;

        // The upload completed but the acknowledgement was lost, so the client re-sends the final
        // chunk from its stale offset. Starting a fresh multipart upload here would complete an
        // object made of that chunk alone and overwrite the finished one.
        var first = Filled(0x41, PartSize);
        var last = Filled(0x42, 4096);
        var file = NewFile(first.Length + last.Length);

        await WriteChunk(file, first, 0);
        await WriteChunk(file, last, PartSize);

        var offset = await WriteChunk(file, last, PartSize);   // replay after completion

        Assert.Equal(file.Size, offset);

        var stored = await ReadAll(file);
        Assert.Equal(file.Size, stored.Length);
        Assert.Equal(first, stored[..PartSize]);
    }

    [Fact]
    public async Task CreateDirectory_LeavesAMarkerThePublishPipelineCanSee()
    {
        if (!Enabled) return;

        // The publish marker is an empty directory telling the shop an order is mid-publish.
        await _storage!.CreateDirectoryAsync("published/1042_in_progress");

        Assert.NotNull(await _storage.ReadFileAsync("published/1042_in_progress/"));
        Assert.Empty(await _storage.GetDirectoryEntriesAsync("published"));   // not a file entry

        await _storage.DeleteDirectoryAsync("published/1042_in_progress");
        Assert.Null(await _storage.ReadFileAsync("published/1042_in_progress/"));
    }

    [Fact]
    public async Task MissingObject_ReadsAsNull()
    {
        if (!Enabled) return;

        Assert.Null(await _storage!.ReadFileAsync("user0/project0/absent.bin"));
    }

    [Fact]
    public async Task DirectoryEntries_AreRecursiveAndRelativeToTheFolder()
    {
        if (!Enabled) return;

        await _storage!.SaveFile("published/order1/spread1.png", Filled(0x01, 512));
        await _storage.SaveFile("published/order1/sub/spread2.png", Filled(0x02, 512));
        await _storage.SaveFile("published/order2/other.png", Filled(0x03, 512));

        var entries = await _storage.GetDirectoryEntriesAsync("published/order1");
        var paths = entries.Select(e => e.Path!).OrderBy(p => p).ToArray();

        Assert.Equal(["/spread1.png", "/sub/spread2.png"], paths);
        Assert.All(entries, e => Assert.Equal(512, e.Size));
    }

    [Fact]
    public async Task OpenFileStream_WritesTheObjectOnDispose_AndTracksPosition()
    {
        if (!Enabled) return;

        var payload = Filled(0x77, 32 * 1024);
        long position;

        using (var stream = _storage!.OpenFileStream("published/order3/spread.png"))
        {
            stream.Write(payload, 0, payload.Length);
            position = stream.Position;   // callers record the byte count from this
        }

        Assert.Equal(payload.Length, position);
        Assert.Equal(payload, await ReadAll(new File { Path = "published/order3/spread.png" }));
    }

    [Fact]
    public async Task DeleteDirectory_RemovesEveryObjectUnderThePrefixOnly()
    {
        if (!Enabled) return;

        await _storage!.SaveFile("wipe/a.bin", Filled(0x01, 16));
        await _storage.SaveFile("wipe/nested/b.bin", Filled(0x02, 16));
        await _storage.SaveFile("keep/c.bin", Filled(0x03, 16));

        await _storage.DeleteDirectoryAsync("wipe");

        Assert.Empty(await _storage.GetDirectoryEntriesAsync("wipe"));
        Assert.Single(await _storage.GetDirectoryEntriesAsync("keep"));
    }

    [Fact]
    public async Task CopyToFile_MaterialisesTheObjectOnTheLocalFilesystem()
    {
        if (!Enabled) return;

        // The publish pipeline hands this temp path to native code and then stats it.
        var payload = Filled(0x5A, 4096);
        var file = NewFile(payload.Length);
        await WriteChunk(file, payload, 0);

        var tempPath = Path.Combine(Path.GetTempPath(), $"s3copy_{Guid.NewGuid():N}.bin");
        try
        {
            Assert.True(await _storage!.CopyToFileAsync(file, tempPath));
            Assert.Equal(payload, await System.IO.File.ReadAllBytesAsync(tempPath));
        }
        finally
        {
            if (System.IO.File.Exists(tempPath))
                System.IO.File.Delete(tempPath);
        }
    }

    [Fact]
    public async Task CopyToFile_ReturnsFalseForAMissingObject()
    {
        if (!Enabled) return;

        var tempPath = Path.Combine(Path.GetTempPath(), $"s3copy_{Guid.NewGuid():N}.bin");

        Assert.False(await _storage!.CopyToFileAsync("user0/project0/absent.bin", tempPath));
        Assert.False(System.IO.File.Exists(tempPath));
    }
}
