using System.Text;
using Azure.Storage.Blobs;

namespace Sencilla.Component.Files.AzureStorage.Tests;

/// <summary>
/// Live integration tests for the "file is not there" contract of <see cref="AzureBlobStorage.ReadFileAsync(string, CancellationToken)"/>.
///
/// The nullable return type promises null for an absent file — LocalDriveStorage delivers that, Azure used to
/// throw a 404 RequestFailedException instead. Every read-then-write caller (append to a log, compare against
/// the previous version) therefore worked on a dev machine and failed on Azure the FIRST time, when the target
/// does not exist yet — and so never got created, so it failed every time after that too.
///
/// Set SENCILLA_TEST_AZURE_CONN to the storage connection string to run. When unset, the tests no-op (so the
/// suite stays green on machines without Azure access).
/// </summary>
public class AzureBlobStorageMissingBlobTests : IDisposable
{
    private readonly string? _conn = Environment.GetEnvironmentVariable("SENCILLA_TEST_AZURE_CONN");
    private readonly List<string> _containers = new();

    private (AzureBlobStorage storage, string container) NewTarget()
    {
        var container = $"readtest{Guid.NewGuid():N}";   // lowercase, valid Azure container name
        _containers.Add(container);

        var options = new AzureBlobStorageOptions { ConnectionString = _conn! };
        return (new AzureBlobStorage(options, new Mock<IFilePathResolver>().Object), container);
    }

    private static async Task Save(AzureBlobStorage storage, string path, string text)
    {
        using var ms = new MemoryStream(Encoding.UTF8.GetBytes(text));
        await storage.SaveFile(path, ms);
    }

    private static async Task<string?> Read(AzureBlobStorage storage, string path)
    {
        await using var stream = await storage.ReadFileAsync(path);
        if (stream == null) return null;
        using var reader = new StreamReader(stream, Encoding.UTF8);
        return await reader.ReadToEndAsync();
    }

    [Fact]
    public async Task MissingBlob_InExistingContainer_ReadsAsNull()
    {
        if (string.IsNullOrEmpty(_conn)) return;   // skip when no Azure access
        var (storage, container) = NewTarget();

        await Save(storage, $"{container}/present.txt", "here");

        Assert.Null(await Read(storage, $"{container}/absent.txt"));
    }

    [Fact]
    public async Task MissingContainer_ReadsAsNull()
    {
        if (string.IsNullOrEmpty(_conn)) return;
        var (storage, container) = NewTarget();   // never created — nothing was written to it

        Assert.Null(await Read(storage, $"{container}/absent.txt"));
    }

    [Fact]
    public async Task ReadThenWrite_AppendsToAFileThatDidNotExistYet()
    {
        if (string.IsNullOrEmpty(_conn)) return;
        var (storage, container) = NewTarget();
        var path = $"{container}/changelog.txt";

        // The publish change log's pattern: read what is there, prepend, write it back.
        for (var version = 1; version <= 3; version++)
            await Save(storage, path, $"v{version}\n" + (await Read(storage, path) ?? ""));

        Assert.Equal("v3\nv2\nv1\n", await Read(storage, path));
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
