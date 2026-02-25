using Microsoft.AspNetCore.Http;

namespace Sencilla.Component.Files.Tests;

/// <summary>
/// Tests for <see cref="DeleteFileHandler"/>.
///
/// Covers: single file deletion, deletion with variants, non-existent file, event publishing.
/// </summary>
public class DeleteFileHandlerTests
{
    private readonly Mock<IFileStorage> _storage = new();
    private readonly Mock<IEventDispatcher> _events = new();
    private readonly Mock<IReadRepository<File, Guid>> _readRepo = new();
    private readonly Mock<IDeleteRepository<File, Guid>> _deleteRepo = new();

    private DeleteFileHandler CreateHandler() =>
        new(_storage.Object, _events.Object, _readRepo.Object, _deleteRepo.Object);

    private static HttpContext CreateDeleteContext(Guid fileId)
    {
        var context = new DefaultHttpContext();
        context.Request.Method = "DELETE";
        context.Request.Path = $"/api/v1/files/upload/{fileId}";
        return context;
    }

    [Fact]
    public async Task Handle_ExistingFileNoVariants_DeletesFileAndReturns204()
    {
        var fileId = Guid.NewGuid();
        var file = new File { Id = fileId, Name = "photo.jpg", Path = "test/photo.jpg" };
        var context = CreateDeleteContext(fileId);

        _readRepo.Setup(r => r.GetById(fileId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(file);
        _readRepo.Setup(r => r.GetAll(It.IsAny<FileFilter>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Array.Empty<File>());
        _storage.Setup(s => s.DeleteFileAsync(It.IsAny<File>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((File? f, CancellationToken _) => f);
        _deleteRepo.Setup(d => d.Delete(It.IsAny<File>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        var handler = CreateHandler();
        await handler.Handle(context, CancellationToken.None);

        Assert.Equal(StatusCodes.Status204NoContent, context.Response.StatusCode);
        _storage.Verify(s => s.DeleteFileAsync(file, It.IsAny<CancellationToken>()), Times.Once);
        _deleteRepo.Verify(d => d.Delete(file, It.IsAny<CancellationToken>()), Times.Once);
        _events.Verify(e => e.PublishAsync(It.IsAny<FileDeletedEvent>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_FileWithVariants_DeletesAllVariantsAndOriginal()
    {
        var fileId = Guid.NewGuid();
        var file = new File { Id = fileId, Name = "photo.jpg", Resolutions = new[] { 100, 200 } };

        var variant1 = new File { Id = Guid.NewGuid(), ParentId = fileId, Dim = 100 };
        var variant2 = new File { Id = Guid.NewGuid(), ParentId = fileId, Dim = 200 };
        var context = CreateDeleteContext(fileId);

        _readRepo.Setup(r => r.GetById(fileId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(file);
        _readRepo.Setup(r => r.GetAll(It.IsAny<FileFilter>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[] { variant1, variant2 });
        _storage.Setup(s => s.DeleteFileAsync(It.IsAny<File>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((File? f, CancellationToken _) => f);
        _deleteRepo.Setup(d => d.Delete(It.IsAny<File>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        var handler = CreateHandler();
        await handler.Handle(context, CancellationToken.None);

        Assert.Equal(StatusCodes.Status204NoContent, context.Response.StatusCode);
        // 2 variants + 1 original = 3 delete calls
        _storage.Verify(s => s.DeleteFileAsync(It.IsAny<File>(), It.IsAny<CancellationToken>()), Times.Exactly(3));
        _deleteRepo.Verify(d => d.Delete(It.IsAny<File>(), It.IsAny<CancellationToken>()), Times.Exactly(3));
    }

    [Fact]
    public async Task Handle_NonExistentFile_Returns400()
    {
        var fileId = Guid.NewGuid();
        var context = CreateDeleteContext(fileId);

        _readRepo.Setup(r => r.GetById(fileId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((File?)null);

        var handler = CreateHandler();
        await handler.Handle(context, CancellationToken.None);

        Assert.Equal(StatusCodes.Status400BadRequest, context.Response.StatusCode);
        _storage.Verify(s => s.DeleteFileAsync(It.IsAny<File>(), It.IsAny<CancellationToken>()), Times.Never);
        _deleteRepo.Verify(d => d.Delete(It.IsAny<File>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_DeletePublishesFileDeletedEvent()
    {
        var fileId = Guid.NewGuid();
        var file = new File { Id = fileId, Name = "report.pdf" };
        var context = CreateDeleteContext(fileId);

        _readRepo.Setup(r => r.GetById(fileId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(file);
        _readRepo.Setup(r => r.GetAll(It.IsAny<FileFilter>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Array.Empty<File>());
        _storage.Setup(s => s.DeleteFileAsync(It.IsAny<File>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((File? f, CancellationToken _) => f);
        _deleteRepo.Setup(d => d.Delete(It.IsAny<File>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        var handler = CreateHandler();
        await handler.Handle(context, CancellationToken.None);

        _events.Verify(e => e.PublishAsync(
            It.Is<FileDeletedEvent>(ev => ev.File!.Id == fileId),
            It.IsAny<CancellationToken>()), Times.Once);
    }
}
