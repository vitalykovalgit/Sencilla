using Microsoft.AspNetCore.Http;

namespace Sencilla.Component.Files.Tests;

/// <summary>
/// Tests for <see cref="HeadFileHandler"/>.
///
/// Covers: file status retrieval, missing TUS header validation, resolution-specific progress.
/// </summary>
public class HeadFileHandlerTests
{
    private readonly Mock<IReadRepository<File, Guid>> _readRepo = new();

    private HeadFileHandler CreateHandler() => new(_readRepo.Object);

    private static HttpContext CreateHeadContext(Guid fileId, int? res = null)
    {
        var context = new DefaultHttpContext();
        context.Request.Method = "HEAD";
        context.Request.Path = $"/api/v1/files/upload/{fileId}";
        context.Request.Headers[FileHeaders.TusResumable] = "1.0.0";
        if (res.HasValue)
            context.Request.QueryString = new QueryString($"?res={res.Value}");
        return context;
    }

    [Fact]
    public async Task Handle_ValidRequest_ReturnsFileSizeAndOffset()
    {
        var fileId = Guid.NewGuid();
        var file = new File { Id = fileId, Name = "photo.jpg", Size = 2048, Uploaded = 1024 };
        var context = CreateHeadContext(fileId);

        _readRepo.Setup(r => r.GetById(fileId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(file);

        var handler = CreateHandler();
        await handler.Handle(context, CancellationToken.None);

        Assert.Equal(StatusCodes.Status200OK, context.Response.StatusCode);
        Assert.Equal("2048", context.Response.Headers[FileHeaders.UploadLength].ToString());
        Assert.Equal("1024", context.Response.Headers[FileHeaders.UploadOffset].ToString());
    }

    [Fact]
    public async Task Handle_MissingTusHeader_Returns400()
    {
        var context = new DefaultHttpContext();
        context.Request.Method = "HEAD";
        context.Request.Path = $"/api/v1/files/upload/{Guid.NewGuid()}";

        var handler = CreateHandler();
        await handler.Handle(context, CancellationToken.None);

        Assert.Equal(StatusCodes.Status400BadRequest, context.Response.StatusCode);
    }

    [Fact]
    public async Task Handle_NonExistentFile_ReturnsZeroSizeAndOffset()
    {
        var fileId = Guid.NewGuid();
        var context = CreateHeadContext(fileId);

        _readRepo.Setup(r => r.GetById(fileId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((File?)null);

        var handler = CreateHandler();
        await handler.Handle(context, CancellationToken.None);

        Assert.Equal(StatusCodes.Status200OK, context.Response.StatusCode);
        Assert.Equal("0", context.Response.Headers[FileHeaders.UploadLength].ToString());
        Assert.Equal("0", context.Response.Headers[FileHeaders.UploadOffset].ToString());
    }

    [Fact]
    public async Task Handle_WithRes_ReturnsResolutionProgress()
    {
        var fileId = Guid.NewGuid();
        var file = new File
        {
            Id = fileId,
            Name = "photo.jpg",
            Size = 50000,
            Uploaded = 50000,
            Res = new Dictionary<string, ResolutionInfo>
            {
                ["600"] = new ResolutionInfo { S = 12000, U = 3000 }
            }
        };
        var context = CreateHeadContext(fileId, res: 600);

        _readRepo.Setup(r => r.GetById(fileId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(file);

        var handler = CreateHandler();
        await handler.Handle(context, CancellationToken.None);

        Assert.Equal(StatusCodes.Status200OK, context.Response.StatusCode);
        Assert.Equal("12000", context.Response.Headers[FileHeaders.UploadLength].ToString());
        Assert.Equal("3000", context.Response.Headers[FileHeaders.UploadOffset].ToString());
    }

    [Fact]
    public async Task Handle_WithResCompleted_FallsBackToFileProgress()
    {
        var fileId = Guid.NewGuid();
        var file = new File
        {
            Id = fileId,
            Name = "photo.jpg",
            Size = 50000,
            Uploaded = 50000,
            Res = new Dictionary<string, ResolutionInfo>
            {
                ["600"] = new ResolutionInfo() // completed, S and U are null
            }
        };
        var context = CreateHeadContext(fileId, res: 600);

        _readRepo.Setup(r => r.GetById(fileId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(file);

        var handler = CreateHandler();
        await handler.Handle(context, CancellationToken.None);

        // Falls back to file-level progress since resolution upload is complete
        Assert.Equal(StatusCodes.Status200OK, context.Response.StatusCode);
        Assert.Equal("50000", context.Response.Headers[FileHeaders.UploadLength].ToString());
        Assert.Equal("50000", context.Response.Headers[FileHeaders.UploadOffset].ToString());
    }

    [Fact]
    public async Task Handle_WithResNotFound_FallsBackToFileProgress()
    {
        var fileId = Guid.NewGuid();
        var file = new File
        {
            Id = fileId,
            Name = "photo.jpg",
            Size = 50000,
            Uploaded = 50000,
            Res = new Dictionary<string, ResolutionInfo>
            {
                ["600"] = new ResolutionInfo { S = 12000, U = 3000 }
            }
        };
        var context = CreateHeadContext(fileId, res: 999);

        _readRepo.Setup(r => r.GetById(fileId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(file);

        var handler = CreateHandler();
        await handler.Handle(context, CancellationToken.None);

        // Falls back to file-level progress since resolution 999 doesn't exist
        Assert.Equal(StatusCodes.Status200OK, context.Response.StatusCode);
        Assert.Equal("50000", context.Response.Headers[FileHeaders.UploadLength].ToString());
    }
}
