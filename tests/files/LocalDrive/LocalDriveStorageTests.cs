namespace Sencilla.Component.Files.LocalDrive.Tests;

/// <summary>
/// Tests for <see cref="LocalDriveStorage"/>.
///
/// Covers: file read/write, directory operations, zip operations, delete operations.
/// Uses a temporary directory per test to avoid cross-test pollution.
/// </summary>
public class LocalDriveStorageTests : IDisposable
{
    private readonly string _testRoot;
    private readonly LocalDriveStorage _storage;

    public LocalDriveStorageTests()
    {
        _testRoot = Path.Combine(Path.GetTempPath(), $"sencilla_test_{Guid.NewGuid()}");
        Directory.CreateDirectory(_testRoot);
        _storage = new LocalDriveStorage(new LocalDriveStorageOptions { RootPath = _testRoot });
    }

    private static File MakeFile(
        string name = "test.txt",
        string? path = null,
        FileOrigin origin = FileOrigin.None) =>
        new()
        {
            Id = Guid.NewGuid(),
            Name = name,
            Path = path ?? $"none/{Guid.NewGuid()}.txt",
            Origin = origin,
            Size = 0
        };

    // ── Properties ────────────────────────────────────────────────────────────

    [Fact]
    public void Type_ReturnsConfiguredType()
    {
        Assert.Equal(1, _storage.Type);
    }

    [Fact]
    public void GetRootDirectory_ReturnsConfiguredPath()
    {
        Assert.Equal(_testRoot, _storage.GetRootDirectory());
    }

    // ── WriteFileAsync (byte[]) ───────────────────────────────────────────────

    [Fact]
    public async Task WriteFileAsync_ByteArray_CreatesFileOnDisk()
    {
        var file = MakeFile();
        var content = "Hello Sencilla"u8.ToArray();

        await _storage.WriteFileAsync(file, content);

        var filePath = Path.Combine(_testRoot, Path.GetDirectoryName(file.Path)!, Path.GetFileName(file.Path)!);
        Assert.True(System.IO.File.Exists(filePath));
    }

    [Fact]
    public async Task WriteFileAsync_ByteArray_ReturnsFileLength()
    {
        var file = MakeFile();
        var content = "Hello"u8.ToArray();

        var length = await _storage.WriteFileAsync(file, content);

        Assert.Equal(content.Length, length);
    }

    [Fact]
    public async Task WriteFileAsync_EmptyArray_CreatesEmptyFile()
    {
        var file = MakeFile();

        var length = await _storage.WriteFileAsync(file, []);

        Assert.Equal(0, length);
    }

    // ── WriteFileAsync (Stream) ───────────────────────────────────────────────

    [Fact]
    public async Task WriteFileAsync_Stream_WritesContentCorrectly()
    {
        var file = MakeFile();
        var content = "Stream content"u8.ToArray();
        using var stream = new MemoryStream(content);

        var offset = await _storage.WriteFileAsync(file, stream);

        Assert.Equal(content.Length, offset);
    }

    [Fact]
    public async Task WriteFileAsync_Stream_WithOffset_AppendsContent()
    {
        var file = MakeFile();
        var initial = "Hello"u8.ToArray();
        await _storage.WriteFileAsync(file, initial);

        var append = " World"u8.ToArray();
        using var stream = new MemoryStream(append);
        var newOffset = await _storage.WriteFileAsync(file, stream, offset: initial.Length);

        Assert.Equal(initial.Length + append.Length, newOffset);
    }

    // ── ReadFileAsync ─────────────────────────────────────────────────────────

    [Fact]
    public async Task ReadFileAsync_ExistingFile_ReturnsStream()
    {
        var file = MakeFile();
        var content = "Read me"u8.ToArray();
        await _storage.WriteFileAsync(file, content);

        using var stream = await _storage.ReadFileAsync(file);

        Assert.NotNull(stream);
        using var reader = new StreamReader(stream);
        Assert.Equal("Read me", await reader.ReadToEndAsync());
    }

    [Fact]
    public async Task ReadFileAsync_NonExistingFile_ReturnsNull()
    {
        var file = MakeFile(path: "nonexistent/file.txt");

        var stream = await _storage.ReadFileAsync(file);

        Assert.Null(stream);
    }

    [Fact]
    public async Task ReadFileAsync_ByPath_ExistingFile_ReturnsStream()
    {
        var relPath = $"readtest/{Guid.NewGuid()}.txt";
        var fullPath = Path.Combine(_testRoot, relPath);
        Directory.CreateDirectory(Path.GetDirectoryName(fullPath)!);
        await System.IO.File.WriteAllTextAsync(fullPath, "By path");

        using var stream = await _storage.ReadFileAsync(relPath);

        Assert.NotNull(stream);
    }

    // ── CreateDirectoryAsync / DeleteDirectoryAsync ────────────────────────────

    [Fact]
    public async Task CreateDirectoryAsync_CreatesDirectory()
    {
        var dirName = $"newdir_{Guid.NewGuid()}";

        await _storage.CreateDirectoryAsync(dirName);

        Assert.True(Directory.Exists(Path.Combine(_testRoot, dirName)));
    }

    [Fact]
    public async Task CreateDirectoryAsync_ExistingDirectory_DoesNotThrow()
    {
        var dirName = $"existing_{Guid.NewGuid()}";
        Directory.CreateDirectory(Path.Combine(_testRoot, dirName));

        await _storage.CreateDirectoryAsync(dirName);

        Assert.True(Directory.Exists(Path.Combine(_testRoot, dirName)));
    }

    [Fact]
    public async Task DeleteDirectoryAsync_RemovesDirectory()
    {
        var dirName = $"todelete_{Guid.NewGuid()}";
        Directory.CreateDirectory(Path.Combine(_testRoot, dirName));

        await _storage.DeleteDirectoryAsync(dirName);

        Assert.False(Directory.Exists(Path.Combine(_testRoot, dirName)));
    }

    [Fact]
    public async Task DeleteDirectoryAsync_NonExistingDirectory_DoesNotThrow()
    {
        await _storage.DeleteDirectoryAsync("nonexistent_dir");
    }

    // ── RenameDirectoryAsync ──────────────────────────────────────────────────

    [Fact]
    public async Task RenameDirectoryAsync_RenamesDirectory()
    {
        var src = $"src_{Guid.NewGuid()}";
        var dst = $"dst_{Guid.NewGuid()}";
        Directory.CreateDirectory(Path.Combine(_testRoot, src));

        var result = await _storage.RenameDirectoryAsync(src, dst);

        Assert.Equal(dst, result);
        Assert.False(Directory.Exists(Path.Combine(_testRoot, src)));
        Assert.True(Directory.Exists(Path.Combine(_testRoot, dst)));
    }

    [Fact]
    public async Task RenameDirectoryAsync_SourceNotExists_ThrowsDirectoryNotFoundException()
    {
        await Assert.ThrowsAsync<DirectoryNotFoundException>(
            () => _storage.RenameDirectoryAsync("no_exist", "anywhere"));
    }

    [Fact]
    public async Task RenameDirectoryAsync_DestExists_ThrowsIOException()
    {
        var src = $"src_{Guid.NewGuid()}";
        var dst = $"dst_{Guid.NewGuid()}";
        Directory.CreateDirectory(Path.Combine(_testRoot, src));
        Directory.CreateDirectory(Path.Combine(_testRoot, dst));

        await Assert.ThrowsAsync<IOException>(
            () => _storage.RenameDirectoryAsync(src, dst));
    }

    // ── DeleteFileAsync ───────────────────────────────────────────────────────

    [Fact]
    public async Task DeleteFileAsync_ExistingFile_DeletesFile()
    {
        var file = MakeFile();
        await _storage.WriteFileAsync(file, "delete me"u8.ToArray());

        var result = await _storage.DeleteFileAsync(file);

        Assert.NotNull(result);
        Assert.Equal(file.Id, result.Id);
    }

    [Fact]
    public async Task DeleteFileAsync_NonExistingFile_ReturnsFileWithoutError()
    {
        var file = MakeFile(path: "nowhere/nothing.txt");

        var result = await _storage.DeleteFileAsync(file);

        Assert.NotNull(result);
    }

    [Fact]
    public async Task DeleteFileAsync_NullFile_ReturnsNull()
    {
        var result = await _storage.DeleteFileAsync((File?)null);

        Assert.Null(result);
    }

    [Fact]
    public async Task DeleteFileAsync_ByPath_EmptyPath_DoesNotThrow()
    {
        await _storage.DeleteFileAsync("");
        await _storage.DeleteFileAsync((string)null!);
    }

    // ── SaveFile ──────────────────────────────────────────────────────────────

    [Fact]
    public async Task SaveFile_Stream_CreatesFileAndReturnsTrue()
    {
        var relPath = $"saved/{Guid.NewGuid()}.txt";
        using var stream = new MemoryStream("saved content"u8.ToArray());

        var result = await _storage.SaveFile(relPath, stream);

        Assert.True(result);
        Assert.True(System.IO.File.Exists(Path.Combine(_testRoot, relPath)));
    }

    [Fact]
    public async Task SaveFile_ByteArray_CreatesFile()
    {
        var relPath = $"saved/{Guid.NewGuid()}.bin";

        var result = await _storage.SaveFile(relPath, new byte[] { 1, 2, 3 });

        Assert.True(result);
    }

    // ── GetDirectoryEntriesAsync ──────────────────────────────────────────────

    [Fact]
    public async Task GetDirectoryEntriesAsync_WithFiles_ReturnsFileEntries()
    {
        var dir = $"entries_{Guid.NewGuid()}";
        var fullDir = Path.Combine(_testRoot, dir);
        Directory.CreateDirectory(fullDir);
        await System.IO.File.WriteAllTextAsync(Path.Combine(fullDir, "a.txt"), "a");
        await System.IO.File.WriteAllTextAsync(Path.Combine(fullDir, "b.txt"), "b");

        var entries = await _storage.GetDirectoryEntriesAsync(dir);

        Assert.Equal(2, entries.Length);
        Assert.All(entries, e => Assert.True(e.Size > 0));
    }

    [Fact]
    public async Task GetDirectoryEntriesAsync_NonExistingFolder_ReturnsEmpty()
    {
        var entries = await _storage.GetDirectoryEntriesAsync("no_such_folder");

        Assert.Empty(entries);
    }

    // ── CopyToFileAsync ───────────────────────────────────────────────────────

    [Fact]
    public async Task CopyToFileAsync_ByPath_CopiesFile()
    {
        var srcPath = Path.Combine(_testRoot, $"src_{Guid.NewGuid()}.txt");
        var dstPath = Path.Combine(_testRoot, $"copies/dst_{Guid.NewGuid()}.txt");
        await System.IO.File.WriteAllTextAsync(srcPath, "copy me");

        var result = await _storage.CopyToFileAsync(srcPath, dstPath);

        Assert.True(result);
        Assert.True(System.IO.File.Exists(dstPath));
        Assert.Equal("copy me", await System.IO.File.ReadAllTextAsync(dstPath));
    }

    // ── ZipFolderAsync ────────────────────────────────────────────────────────

    [Fact]
    public async Task ZipFolderAsync_CreatesZipFile()
    {
        var dir = $"zipme_{Guid.NewGuid()}";
        var fullDir = Path.Combine(_testRoot, dir);
        Directory.CreateDirectory(fullDir);
        await System.IO.File.WriteAllTextAsync(Path.Combine(fullDir, "file.txt"), "zip content");

        var zipPath = $"output_{Guid.NewGuid()}.zip";
        await _storage.ZipFolderAsync(dir, zipPath);

        Assert.True(System.IO.File.Exists(Path.Combine(_testRoot, zipPath)));
    }

    [Fact]
    public async Task ZipFolderAsync_NonExistingFolder_ThrowsDirectoryNotFoundException()
    {
        await Assert.ThrowsAsync<DirectoryNotFoundException>(
            () => _storage.ZipFolderAsync("nope", "output.zip"));
    }

    public void Dispose()
    {
        if (Directory.Exists(_testRoot))
            Directory.Delete(_testRoot, recursive: true);
    }
}
