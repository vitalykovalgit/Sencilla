using Microsoft.AspNetCore.Http;
using System.Linq.Expressions;

namespace Sencilla.Component.Files.Tests;

/// <summary>
/// Tests for <see cref="CreateFileHandler"/>.
///
/// Covers: standard file creation, idempotency, validation, resolution uploads.
/// </summary>
public class CreateFileHandlerTests
{
    private readonly Mock<IFileStorage> _storage = new();
    private readonly Mock<IEventDispatcher> _events = new();
    private readonly Mock<IFilePathResolver> _pathResolver = new();
    private readonly Mock<ICreateRepository<File, Guid>> _createRepo = new();
    private readonly Mock<IMergeRepository<File, Guid>> _resMergeRepo = new();

    private CreateFileHandler CreateHandler() =>
        new(_storage.Object, _events.Object, _pathResolver.Object, _createRepo.Object, _resMergeRepo.Object);

    private static HttpContext CreateHttpContext(long uploadLength, string metadata)
    {
        var context = new DefaultHttpContext();
        context.Request.Method = "POST";
        context.Request.Path = "/api/v1/files/upload";
        context.Request.Headers[FileHeaders.UploadLength] = uploadLength.ToString();
        context.Request.Headers[FileHeaders.UploadDeferLength] = "1";
        context.Request.Headers[FileHeaders.UploadMetadata] = metadata;
        return context;
    }

    private static string EncodeMetadata(params (string key, string value)[] pairs)
    {
        return string.Join(",", pairs.Select(p => $"{p.key} {Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(p.value))}"));
    }

    [Fact]
    public async Task Handle_FirstUpload_CreatesFileAndReturns201()
    {
        var fileId = Guid.NewGuid();
        var metadata = EncodeMetadata(("id", fileId.ToString()), ("name", "photo.jpg"), ("size", "1024"));
        var context = CreateHttpContext(1024, metadata);

        _createRepo.Setup(r => r.GetById(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((File?)null);
        _createRepo.Setup(r => r.Create(It.IsAny<File>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((File f, CancellationToken _) => f);
        _pathResolver.Setup(p => p.GetFullPath(It.IsAny<File>())).Returns("test/path.jpg");
        _storage.Setup(s => s.WriteFileAsync(It.IsAny<File>(), It.IsAny<byte[]>(), 0, It.IsAny<CancellationToken>()))
            .ReturnsAsync(0L);

        var handler = CreateHandler();
        await handler.Handle(context, CancellationToken.None);

        Assert.Equal(StatusCodes.Status201Created, context.Response.StatusCode);
        _createRepo.Verify(r => r.Create(It.IsAny<File>(), It.IsAny<CancellationToken>()), Times.Once);
        _events.Verify(e => e.PublishAsync(It.IsAny<FileCreatedEvent>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_ExistingFile_ReturnsLocationWithoutCreating()
    {
        var fileId = Guid.NewGuid();
        var existingFile = new File { Id = fileId, Name = "photo.jpg" };
        var metadata = EncodeMetadata(("id", fileId.ToString()), ("name", "photo.jpg"), ("size", "1024"));
        var context = CreateHttpContext(1024, metadata);

        _createRepo.Setup(r => r.GetById(fileId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingFile);
        _pathResolver.Setup(p => p.GetFullPath(It.IsAny<File>())).Returns("test/path.jpg");

        var handler = CreateHandler();
        await handler.Handle(context, CancellationToken.None);

        Assert.Equal(StatusCodes.Status201Created, context.Response.StatusCode);
        _createRepo.Verify(r => r.Create(It.IsAny<File>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_MissingUploadLength_Returns400()
    {
        var context = new DefaultHttpContext();
        context.Request.Headers[FileHeaders.UploadDeferLength] = "1";

        var handler = CreateHandler();
        await handler.Handle(context, CancellationToken.None);

        Assert.Equal(StatusCodes.Status400BadRequest, context.Response.StatusCode);
    }

    [Fact]
    public async Task Handle_MissingMetadata_Returns400()
    {
        var context = new DefaultHttpContext();
        context.Request.Headers[FileHeaders.UploadLength] = "1024";
        context.Request.Headers[FileHeaders.UploadDeferLength] = "1";

        var handler = CreateHandler();
        await handler.Handle(context, CancellationToken.None);

        Assert.Equal(StatusCodes.Status400BadRequest, context.Response.StatusCode);
    }

    [Fact]
    public async Task Handle_MissingFileName_Returns400()
    {
        var metadata = EncodeMetadata(("size", "1024"));
        var context = CreateHttpContext(1024, metadata);

        var handler = CreateHandler();
        await handler.Handle(context, CancellationToken.None);

        Assert.Equal(StatusCodes.Status400BadRequest, context.Response.StatusCode);
    }

    [Fact]
    public async Task Handle_InvalidUploadDeferLength_Returns400()
    {
        var context = new DefaultHttpContext();
        context.Request.Headers[FileHeaders.UploadLength] = "1024";
        context.Request.Headers[FileHeaders.UploadDeferLength] = "0";
        context.Request.Headers[FileHeaders.UploadMetadata] = EncodeMetadata(("name", "test.jpg"));

        var handler = CreateHandler();
        await handler.Handle(context, CancellationToken.None);

        Assert.Equal(StatusCodes.Status400BadRequest, context.Response.StatusCode);
    }

    [Fact]
    public async Task Handle_CreatedFile_LocationHeaderContainsFileId()
    {
        var fileId = Guid.NewGuid();
        var metadata = EncodeMetadata(("id", fileId.ToString()), ("name", "photo.jpg"));
        var context = CreateHttpContext(1024, metadata);

        _createRepo.Setup(r => r.GetById(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((File?)null);
        _createRepo.Setup(r => r.Create(It.IsAny<File>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((File f, CancellationToken _) => f);
        _pathResolver.Setup(p => p.GetFullPath(It.IsAny<File>())).Returns("test/path.jpg");
        _storage.Setup(s => s.WriteFileAsync(It.IsAny<File>(), It.IsAny<byte[]>(), 0, It.IsAny<CancellationToken>()))
            .ReturnsAsync(0L);

        var handler = CreateHandler();
        await handler.Handle(context, CancellationToken.None);

        var location = context.Response.Headers["Location"].ToString();
        Assert.Contains(fileId.ToString(), location);
    }

    [Fact]
    public async Task Handle_NoIdInMetadata_GeneratesNewId()
    {
        var metadata = EncodeMetadata(("name", "photo.jpg"), ("size", "1024"));
        var context = CreateHttpContext(1024, metadata);

        File? createdFile = null;
        _createRepo.Setup(r => r.GetById(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((File?)null);
        _createRepo.Setup(r => r.Create(It.IsAny<File>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((File f, CancellationToken _) => { createdFile = f; return f; });
        _pathResolver.Setup(p => p.GetFullPath(It.IsAny<File>())).Returns("test/path.jpg");
        _storage.Setup(s => s.WriteFileAsync(It.IsAny<File>(), It.IsAny<byte[]>(), 0, It.IsAny<CancellationToken>()))
            .ReturnsAsync(0L);

        var handler = CreateHandler();
        await handler.Handle(context, CancellationToken.None);

        Assert.NotNull(createdFile);
        Assert.NotEqual(Guid.Empty, createdFile!.Id);
    }

    [Fact]
    public async Task Handle_NewFileWithRes_CreatesFileWithResColumn()
    {
        var fileId = Guid.NewGuid();
        var metadata = EncodeMetadata(("id", fileId.ToString()), ("name", "photo.jpg"), ("size", "120034"), (nameof(File.Res).ToLowerInvariant(),"600"));
        var context = CreateHttpContext(120034, metadata);

        File? createdFile = null;
        _createRepo.Setup(r => r.GetById(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((File?)null);
        _createRepo.Setup(r => r.Create(It.IsAny<File>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((File f, CancellationToken _) => { createdFile = f; return f; });
        _pathResolver.Setup(p => p.GetFullPath(It.IsAny<File>())).Returns("test/path.jpg");
        _pathResolver.Setup(p => p.GetResolutionPath(It.IsAny<File>(), 600)).Returns("test/path_600.jpg");
        _storage.Setup(s => s.WriteFileAsync(It.IsAny<File>(), It.IsAny<byte[]>(), 0, It.IsAny<CancellationToken>()))
            .ReturnsAsync(0L);

        var handler = CreateHandler();
        await handler.Handle(context, CancellationToken.None);

        Assert.Equal(StatusCodes.Status201Created, context.Response.StatusCode);
        Assert.NotNull(createdFile);
        Assert.NotNull(createdFile!.Res);
        Assert.True(createdFile.Res.ContainsKey("600"));
        Assert.Equal(120034, createdFile.Res["600"].S);
        Assert.Equal(0, createdFile.Res["600"].U);
    }

    [Fact]
    public async Task Handle_NewFileWithRes_LocationContainsResQueryParam()
    {
        var fileId = Guid.NewGuid();
        var metadata = EncodeMetadata(("id", fileId.ToString()), ("name", "photo.jpg"), (nameof(File.Res).ToLowerInvariant(),"600"));
        var context = CreateHttpContext(120034, metadata);

        _createRepo.Setup(r => r.GetById(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((File?)null);
        _createRepo.Setup(r => r.Create(It.IsAny<File>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((File f, CancellationToken _) => f);
        _pathResolver.Setup(p => p.GetFullPath(It.IsAny<File>())).Returns("test/path.jpg");
        _pathResolver.Setup(p => p.GetResolutionPath(It.IsAny<File>(), 600)).Returns("test/path_600.jpg");
        _storage.Setup(s => s.WriteFileAsync(It.IsAny<File>(), It.IsAny<byte[]>(), 0, It.IsAny<CancellationToken>()))
            .ReturnsAsync(0L);

        var handler = CreateHandler();
        await handler.Handle(context, CancellationToken.None);

        var location = context.Response.Headers["Location"].ToString();
        Assert.Contains("?res=600", location);
    }

    [Fact]
    public async Task Handle_ExistingFileWithRes_UpdatesResColumnWithoutCreating()
    {
        var fileId = Guid.NewGuid();
        var existingFile = new File
        {
            Id = fileId,
            Name = "photo.jpg",
            Path = "test/path.jpg",
            Storage = 1,
            Res = new Dictionary<string, ResolutionInfo>
            {
                ["1000"] = new ResolutionInfo()
            }
        };
        var metadata = EncodeMetadata(("id", fileId.ToString()), ("name", "photo.jpg"), ("size", "5200"), (nameof(File.Res).ToLowerInvariant(),"100"));
        var context = CreateHttpContext(5200, metadata);

        _createRepo.Setup(r => r.GetById(fileId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingFile);
        _pathResolver.Setup(p => p.GetFullPath(It.IsAny<File>())).Returns("test/path.jpg");
        _pathResolver.Setup(p => p.GetResolutionPath(existingFile, 100)).Returns("test/path_100.jpg");
        _storage.Setup(s => s.WriteFileAsync(It.IsAny<File>(), It.IsAny<byte[]>(), 0, It.IsAny<CancellationToken>()))
            .ReturnsAsync(0L);

        var handler = CreateHandler();
        await handler.Handle(context, CancellationToken.None);

        Assert.Equal(StatusCodes.Status201Created, context.Response.StatusCode);
        _createRepo.Verify(r => r.Create(It.IsAny<File>(), It.IsAny<CancellationToken>()), Times.Never);
        _resMergeRepo.Verify(r => r.MergeAsync(
            fileId,
            It.IsAny<Expression<Func<File, IDictionary<string, ResolutionInfo>?>>>(),
            "100",
            It.Is<ResolutionInfo>(i => i.S == 5200 && i.U == 0),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_ExistingFileWithRes_WritesEmptyResolutionFile()
    {
        var fileId = Guid.NewGuid();
        var existingFile = new File { Id = fileId, Name = "photo.jpg", Path = "test/path.jpg", Storage = 1 };
        var metadata = EncodeMetadata(("id", fileId.ToString()), ("name", "photo.jpg"), ("size", "5200"), (nameof(File.Res).ToLowerInvariant(),"500"));
        var context = CreateHttpContext(5200, metadata);

        _createRepo.Setup(r => r.GetById(fileId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingFile);
        _pathResolver.Setup(p => p.GetFullPath(It.IsAny<File>())).Returns("test/path.jpg");
        _pathResolver.Setup(p => p.GetResolutionPath(existingFile, 500)).Returns("test/path_500.jpg");
        _storage.Setup(s => s.WriteFileAsync(It.IsAny<File>(), It.IsAny<byte[]>(), 0, It.IsAny<CancellationToken>()))
            .ReturnsAsync(0L);

        var handler = CreateHandler();
        await handler.Handle(context, CancellationToken.None);

        _storage.Verify(s => s.WriteFileAsync(
            It.Is<File>(f => f.Path == "test/path_500.jpg"),
            It.IsAny<byte[]>(), 0, It.IsAny<CancellationToken>()), Times.Once);
    }
}
