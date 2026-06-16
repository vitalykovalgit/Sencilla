using Azure.Storage.Blobs;

namespace Sencilla.Component.Files.AzureStorage.Tests;

/// <summary>
/// Live integration tests for <see cref="AzureBlobStorage"/> chunked-upload semantics
/// against a real Azure Blob account (staging). Each test uses a throwaway container
/// that is deleted on dispose.
///
/// Reproduces the TUS dropped-ack retry that corrupted uploads: a chunk acknowledged
/// by Azure but whose response never reached the client gets re-sent from the last
/// committed offset. The fix uses IfAppendPositionEqual so the duplicate is rejected
/// (412) and skipped instead of being appended twice.
///
/// Set SENCILLA_TEST_AZURE_CONN to the storage connection string to run. When unset,
/// the tests no-op (so the suite stays green on machines without Azure access).
/// </summary>
public class AzureBlobStorageUploadIdempotencyTests : IDisposable
{
    private const int MB = 1024 * 1024;

    private readonly string? _conn = Environment.GetEnvironmentVariable("SENCILLA_TEST_AZURE_CONN");
    private readonly List<string> _containers = new();

    private static byte[] Filled(byte value, int size)
    {
        var b = new byte[size];
        Array.Fill(b, value);
        return b;
    }

    private (AzureBlobStorage storage, File file, string container) NewTarget()
    {
        var container = $"upltest{Guid.NewGuid():N}";   // lowercase, valid Azure container name
        _containers.Add(container);

        var options = new AzureBlobStorageOptions { ConnectionString = _conn! };
        var storage = new AzureBlobStorage(options, new Mock<IFilePathResolver>().Object);
        var file = new File { Id = Guid.NewGuid(), Name = "orig.bin", Path = $"{container}/none/orig.bin" };
        return (storage, file, container);
    }

    private static async Task<long> WriteChunk(AzureBlobStorage storage, File file, byte[] data, long offset)
    {
        using var ms = new MemoryStream(data);
        return await storage.WriteFileAsync(file, ms, offset, data.Length);
    }

    private static async Task<byte[]> ReadAll(AzureBlobStorage storage, File file)
    {
        await using var s = await storage.ReadFileAsync(file);
        using var ms = new MemoryStream();
        await s!.CopyToAsync(ms);
        return ms.ToArray();
    }

    [Fact]
    public async Task ResentChunk_IsNotAppendedTwice()
    {
        if (string.IsNullOrEmpty(_conn)) return;   // skip when no Azure access
        var (storage, file, _) = NewTarget();

        var c1 = Filled(0x01, MB);
        var c2 = Filled(0x02, MB);
        var c3 = Filled(0x03, MB);

        // Sequential TUS chunks...
        var o1 = await WriteChunk(storage, file, c1, 0);
        Assert.Equal(MB, o1);

        var o2 = await WriteChunk(storage, file, c2, o1);
        Assert.Equal(2L * MB, o2);

        // ...with chunk 2 re-sent (dropped-ack retry) at the SAME offset.
        var o2Retry = await WriteChunk(storage, file, c2, o1);
        Assert.Equal(2L * MB, o2Retry);          // returns expected offset, blob not grown

        var o3 = await WriteChunk(storage, file, c3, o2);
        Assert.Equal(3L * MB, o3);

        // Blob is exactly c1+c2+c3 — the resend did NOT duplicate bytes.
        var blob = await ReadAll(storage, file);
        Assert.Equal(3L * MB, blob.Length);
        Assert.All(blob[..MB], b => Assert.Equal(0x01, b));
        Assert.All(blob[MB..(2 * MB)], b => Assert.Equal(0x02, b));
        Assert.All(blob[(2 * MB)..(3 * MB)], b => Assert.Equal(0x03, b));
    }

    [Fact]
    public async Task RestartFromOffsetZero_OverwritesStalePartialBlob()
    {
        if (string.IsNullOrEmpty(_conn)) return;
        var (storage, file, _) = NewTarget();

        // First attempt leaves a partial blob...
        await WriteChunk(storage, file, Filled(0x01, MB), 0);
        await WriteChunk(storage, file, Filled(0x01, MB), MB);   // 2 MB committed

        // ...client restarts the whole upload from offset 0 with different content.
        var newFirst = Filled(0x09, MB);
        var off = await WriteChunk(storage, file, newFirst, 0);
        Assert.Equal(MB, off);

        // Blob is exactly the restarted content — no leftover bytes from the first attempt.
        var blob = await ReadAll(storage, file);
        Assert.Equal(MB, blob.Length);
        Assert.All(blob, b => Assert.Equal(0x09, b));
    }

    public void Dispose()
    {
        if (string.IsNullOrEmpty(_conn)) return;
        var svc = new BlobServiceClient(_conn);
        foreach (var c in _containers)
        {
            try { svc.GetBlobContainerClient(c).DeleteIfExists(); }
            catch { /* best-effort cleanup */ }
        }
    }
}
