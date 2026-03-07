using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Sencilla.Component.Files.Tests;

/// <summary>
/// Tests for <see cref="FileStreamController"/>.
///
/// Covers: resolution stream retrieval, missing resolution returns 400, valid resolution returns stream.
/// </summary>
public class FileStreamControllerTests
{
    private readonly Mock<IServiceProvider> _provider = new();
    private readonly Mock<IReadRepository<File, Guid>> _fileRepo = new();
    private readonly Mock<IFilePathResolver> _pathResolver = new();
    private readonly Mock<IFileStorage> _storage = new();

    private FileStreamController CreateController()
    {
        _provider.Setup(p => p.GetService(typeof(IFileStorage)))
            .Returns(_storage.Object);

        var controller = new FileStreamController(_provider.Object, _fileRepo.Object, _pathResolver.Object);
        controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext()
        };
        return controller;
    }

    [Fact]
    public async Task GetFileStream_WithResThatExists_ReturnsFileStream()
    {
        var fileId = Guid.NewGuid();
        var file = new File
        {
            Id = fileId,
            Name = "photo.jpg",
            MimeType = "image/jpeg",
            Path = "test/photo.jpg",
            Storage = 0,
            Res = new Dictionary<string, ResolutionInfo>
            {
                ["600"] = new ResolutionInfo()
            }
        };

        _fileRepo.Setup(r => r.GetById(fileId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(file);
        _pathResolver.Setup(p => p.GetResolutionPath(file, 600)).Returns("test/photo_600.jpg");
        _storage.Setup(s => s.ReadFileAsync(It.Is<File>(f => f.Path == "test/photo_600.jpg"), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new MemoryStream(new byte[100]));

        var controller = CreateController();
        var result = await controller.GetFileStream(fileId, dim: null, res: 600, CancellationToken.None);

        Assert.IsType<FileStreamResult>(result);
    }

    [Fact]
    public async Task GetFileStream_WithResThatDoesNotExist_Returns400()
    {
        var fileId = Guid.NewGuid();
        var file = new File
        {
            Id = fileId,
            Name = "photo.jpg",
            Path = "test/photo.jpg",
            Res = new Dictionary<string, ResolutionInfo>
            {
                ["600"] = new ResolutionInfo()
            }
        };

        _fileRepo.Setup(r => r.GetById(fileId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(file);

        var controller = CreateController();
        var result = await controller.GetFileStream(fileId, dim: null, res: 999, CancellationToken.None);

        var badRequest = Assert.IsType<BadRequestObjectResult>(result);
        Assert.Contains("999", badRequest.Value!.ToString());
    }

    [Fact]
    public async Task GetFileStream_WithResButNoResColumn_Returns400()
    {
        var fileId = Guid.NewGuid();
        var file = new File
        {
            Id = fileId,
            Name = "photo.jpg",
            Path = "test/photo.jpg",
            Res = null
        };

        _fileRepo.Setup(r => r.GetById(fileId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(file);

        var controller = CreateController();
        var result = await controller.GetFileStream(fileId, dim: null, res: 600, CancellationToken.None);

        Assert.IsType<BadRequestObjectResult>(result);
    }

    [Fact]
    public async Task GetFileStream_WithResFileNotFound_ReturnsNotFound()
    {
        var fileId = Guid.NewGuid();

        _fileRepo.Setup(r => r.GetById(fileId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((File?)null);

        var controller = CreateController();
        var result = await controller.GetFileStream(fileId, dim: null, res: 600, CancellationToken.None);

        Assert.IsType<NotFoundResult>(result);
    }

    [Fact]
    public async Task GetFileStream_WithoutRes_UsesDimLookup()
    {
        var fileId = Guid.NewGuid();
        var file = new File
        {
            Id = Guid.NewGuid(),
            Name = "photo.jpg",
            MimeType = "image/jpeg",
            Path = "test/photo.jpg",
            Storage = 0
        };

        _fileRepo.Setup(r => r.FirstOrDefault(It.IsAny<FileFilter>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(file);
        _storage.Setup(s => s.ReadFileAsync(file, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new MemoryStream(new byte[100]));

        var controller = CreateController();
        var result = await controller.GetFileStream(fileId, dim: 200, res: null, CancellationToken.None);

        Assert.IsType<FileStreamResult>(result);
        _fileRepo.Verify(r => r.FirstOrDefault(It.IsAny<FileFilter>(), It.IsAny<CancellationToken>()), Times.Once);
        _fileRepo.Verify(r => r.GetById(It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
    }
}
