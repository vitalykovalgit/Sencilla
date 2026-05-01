using Microsoft.AspNetCore.Http;
using System.Linq.Expressions;

namespace Sencilla.Component.Files.Tests;

/// <summary>
/// Tests for <see cref="UploadFileHandler"/>.
///
/// Covers: chunk upload, offset validation, upload completion event, resolution uploads.
/// </summary>
public class UploadFileHandlerTests
{
    private readonly Mock<IEventDispatcher> _events = new();
    private readonly Mock<IFileStorage> _storage = new();
    private readonly Mock<IFilePathResolver> _pathResolver = new();
    private readonly Mock<IReadRepository<File, Guid>> _readRepo = new();
    private readonly Mock<IUpdateRepository<FileUpload, Guid>> _uploadRepo = new();
    private readonly Mock<IMergeRepository<File, Guid>> _resMergeRepo = new();

    private UploadFileHandler CreateHandler() =>
        new(_events.Object, _storage.Object, _pathResolver.Object, _readRepo.Object, _uploadRepo.Object, _resMergeRepo.Object);

    private static HttpContext CreatePatchContext(Guid fileId, long offset, byte[] body, int? res = null)
    {
        var context = new DefaultHttpContext();
        context.Request.Method = "PATCH";
        context.Request.Path = $"/api/v1/files/upload/{fileId}";
        context.Request.Headers[FileHeaders.UploadOffset] = offset.ToString();
        context.Request.Body = new MemoryStream(body);
        context.Request.ContentLength = body.Length;
        if (res.HasValue)
            context.Request.QueryString = new QueryString($"?res={res.Value}");
        return context;
    }

    [Fact]
    public async Task Handle_ValidUpload_WritesChunkAndReturns204()
    {
        var fileId = Guid.NewGuid();
        var file = new File { Id = fileId, Name = "photo.jpg", Size = 2048 };
        var chunk = new byte[1024];
        var context = CreatePatchContext(fileId, 0, chunk);

        _readRepo.Setup(r => r.GetById(fileId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(file);
        _storage.Setup(s => s.WriteFileAsync(file, It.IsAny<Stream>(), 0, 1024, It.IsAny<CancellationToken>()))
            .ReturnsAsync(1024L);
        _uploadRepo.Setup(u => u.Update(It.IsAny<FileUpload>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new FileUpload { Id = fileId, Uploaded = 1024 });

        var handler = CreateHandler();
        await handler.Handle(context, CancellationToken.None);

        Assert.Equal(StatusCodes.Status204NoContent, context.Response.StatusCode);
        Assert.Equal("1024", context.Response.Headers[FileHeaders.UploadOffset].ToString());
    }

    [Fact]
    public async Task Handle_MissingUploadOffset_Returns400()
    {
        var context = new DefaultHttpContext();
        context.Request.Method = "PATCH";
        context.Request.Path = $"/api/v1/files/upload/{Guid.NewGuid()}";

        var handler = CreateHandler();
        await handler.Handle(context, CancellationToken.None);

        Assert.Equal(StatusCodes.Status400BadRequest, context.Response.StatusCode);
    }

    [Fact]
    public async Task Handle_NonExistentFile_Returns400()
    {
        var fileId = Guid.NewGuid();
        var context = CreatePatchContext(fileId, 0, new byte[100]);

        _readRepo.Setup(r => r.GetById(fileId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((File?)null);

        var handler = CreateHandler();
        await handler.Handle(context, CancellationToken.None);

        Assert.Equal(StatusCodes.Status400BadRequest, context.Response.StatusCode);
    }

    [Fact]
    public async Task Handle_UploadComplete_PublishesFileUploadedEvent()
    {
        var fileId = Guid.NewGuid();
        var file = new File { Id = fileId, Name = "photo.jpg", Size = 1024 };
        var chunk = new byte[1024];
        var context = CreatePatchContext(fileId, 0, chunk);

        _readRepo.Setup(r => r.GetById(fileId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(file);
        _storage.Setup(s => s.WriteFileAsync(file, It.IsAny<Stream>(), 0, 1024, It.IsAny<CancellationToken>()))
            .ReturnsAsync(1024L);
        _uploadRepo.Setup(u => u.Update(It.IsAny<FileUpload>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new FileUpload { Id = fileId, Uploaded = 1024 });

        var handler = CreateHandler();
        await handler.Handle(context, CancellationToken.None);

        _events.Verify(e => e.PublishAsync(It.IsAny<FileUploadedEvent>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_PartialUpload_DoesNotPublishEvent()
    {
        var fileId = Guid.NewGuid();
        var file = new File { Id = fileId, Name = "photo.jpg", Size = 2048 };
        var chunk = new byte[1024];
        var context = CreatePatchContext(fileId, 0, chunk);

        _readRepo.Setup(r => r.GetById(fileId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(file);
        _storage.Setup(s => s.WriteFileAsync(file, It.IsAny<Stream>(), 0, 1024, It.IsAny<CancellationToken>()))
            .ReturnsAsync(1024L);
        _uploadRepo.Setup(u => u.Update(It.IsAny<FileUpload>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new FileUpload { Id = fileId, Uploaded = 1024 });

        var handler = CreateHandler();
        await handler.Handle(context, CancellationToken.None);

        _events.Verify(e => e.PublishAsync(It.IsAny<FileUploadedEvent>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_ResUpload_WritesToResolutionPath()
    {
        var fileId = Guid.NewGuid();
        var file = new File
        {
            Id = fileId,
            Name = "photo.jpg",
            Path = "test/path.jpg",
            Size = 2048,
            Res = new Dictionary<string, ResolutionInfo>
            {
                ["600"] = new ResolutionInfo { S = 5000, U = 0 }
            }
        };
        var chunk = new byte[1024];
        var context = CreatePatchContext(fileId, 0, chunk, res: 600);

        _readRepo.Setup(r => r.GetById(fileId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(file);
        _pathResolver.Setup(p => p.GetResolutionPath(file, 600)).Returns("test/path_600.jpg");
        _storage.Setup(s => s.WriteFileAsync(It.IsAny<File>(), It.IsAny<Stream>(), 0, 1024, It.IsAny<CancellationToken>()))
            .ReturnsAsync(1024L);

        var handler = CreateHandler();
        await handler.Handle(context, CancellationToken.None);

        Assert.Equal(StatusCodes.Status204NoContent, context.Response.StatusCode);
        _storage.Verify(s => s.WriteFileAsync(
            It.Is<File>(f => f.Path == "test/path_600.jpg"),
            It.IsAny<Stream>(), 0, 1024, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_ResUploadComplete_ClearsResInfoAndPublishesEvent()
    {
        var fileId = Guid.NewGuid();
        var file = new File
        {
            Id = fileId,
            Name = "photo.jpg",
            Path = "test/path.jpg",
            Size = 2048,
            Res = new Dictionary<string, ResolutionInfo>
            {
                ["600"] = new ResolutionInfo { S = 1024, U = 0 }
            }
        };
        var chunk = new byte[1024];
        var context = CreatePatchContext(fileId, 0, chunk, res: 600);

        _readRepo.Setup(r => r.GetById(fileId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(file);
        _pathResolver.Setup(p => p.GetResolutionPath(file, 600)).Returns("test/path_600.jpg");
        _storage.Setup(s => s.WriteFileAsync(It.IsAny<File>(), It.IsAny<Stream>(), 0, 1024, It.IsAny<CancellationToken>()))
            .ReturnsAsync(1024L);

        var handler = CreateHandler();
        await handler.Handle(context, CancellationToken.None);

        _resMergeRepo.Verify(r => r.MergeAsync(
            fileId,
            It.IsAny<Expression<Func<File, IDictionary<string, ResolutionInfo>?>>>(),
            "600",
            It.Is<ResolutionInfo>(i => i.S == null && i.U == null),
            It.IsAny<CancellationToken>()), Times.Once);
        _events.Verify(e => e.PublishAsync(
            It.Is<FileUploadedEvent>(ev => ev.Resolution == 600),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_ResUploadPartial_UpdatesProgress()
    {
        var fileId = Guid.NewGuid();
        var file = new File
        {
            Id = fileId,
            Name = "photo.jpg",
            Path = "test/path.jpg",
            Size = 2048,
            Res = new Dictionary<string, ResolutionInfo>
            {
                ["600"] = new ResolutionInfo { S = 5000, U = 0 }
            }
        };
        var chunk = new byte[1024];
        var context = CreatePatchContext(fileId, 0, chunk, res: 600);

        _readRepo.Setup(r => r.GetById(fileId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(file);
        _pathResolver.Setup(p => p.GetResolutionPath(file, 600)).Returns("test/path_600.jpg");
        _storage.Setup(s => s.WriteFileAsync(It.IsAny<File>(), It.IsAny<Stream>(), 0, 1024, It.IsAny<CancellationToken>()))
            .ReturnsAsync(1024L);

        var handler = CreateHandler();
        await handler.Handle(context, CancellationToken.None);

        _resMergeRepo.Verify(r => r.MergeAsync(
            fileId,
            It.IsAny<Expression<Func<File, IDictionary<string, ResolutionInfo>?>>>(),
            "600",
            It.Is<ResolutionInfo>(i => i.S == 5000 && i.U == 1024),
            It.IsAny<CancellationToken>()), Times.Once);
        _events.Verify(e => e.PublishAsync(It.IsAny<FileUploadedEvent>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_ResUploadUnregisteredRes_Returns400()
    {
        var fileId = Guid.NewGuid();
        var file = new File
        {
            Id = fileId,
            Name = "photo.jpg",
            Path = "test/path.jpg",
            Size = 2048,
            Res = new Dictionary<string, ResolutionInfo>
            {
                ["600"] = new ResolutionInfo { S = 5000, U = 0 }
            }
        };
        var chunk = new byte[100];
        var context = CreatePatchContext(fileId, 0, chunk, res: 999);

        _readRepo.Setup(r => r.GetById(fileId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(file);

        var handler = CreateHandler();
        await handler.Handle(context, CancellationToken.None);

        Assert.Equal(StatusCodes.Status400BadRequest, context.Response.StatusCode);
    }
}
