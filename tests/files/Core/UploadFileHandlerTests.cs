using Microsoft.AspNetCore.Http;

namespace Sencilla.Component.Files.Tests;

/// <summary>
/// Tests for <see cref="UploadFileHandler"/>.
///
/// Covers: chunk upload, offset validation, upload completion event.
/// </summary>
public class UploadFileHandlerTests
{
    private readonly Mock<IEventDispatcher> _events = new();
    private readonly Mock<IFileStorage> _storage = new();
    private readonly Mock<IReadRepository<File, Guid>> _readRepo = new();
    private readonly Mock<IUpdateRepository<FileUpload, Guid>> _uploadRepo = new();

    private UploadFileHandler CreateHandler() =>
        new(_events.Object, _storage.Object, _readRepo.Object, _uploadRepo.Object);

    private static HttpContext CreatePatchContext(Guid fileId, long offset, byte[] body)
    {
        var context = new DefaultHttpContext();
        context.Request.Method = "PATCH";
        context.Request.Path = $"/api/v1/files/upload/{fileId}";
        context.Request.Headers[FileHeaders.UploadOffset] = offset.ToString();
        context.Request.Body = new MemoryStream(body);
        context.Request.ContentLength = body.Length;
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
}
