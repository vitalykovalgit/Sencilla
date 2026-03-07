namespace Sencilla.Component.Files.Tests;

/// <summary>
/// Tests for <see cref="FilePathResolver"/>.
///
/// Covers: path generation for different origins, dimension suffix, special character removal.
/// </summary>
public class FilePathResolverTests
{
    private readonly FilePathResolver _resolver = new();

    [Fact]
    public void GetFullPath_UserOrigin_IncludesUserDirectory()
    {
        var file = new File
        {
            Id = Guid.Parse("aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee"),
            Name = "photo.jpg",
            UserId = 5,
            Origin = FileOrigin.User
        };

        var path = _resolver.GetFullPath(file);

        Assert.Contains("user5", path);
        Assert.EndsWith(".jpg", path);
    }

    [Fact]
    public void GetFullPath_UserOrigin_WithProjectId_IncludesProjectDirectory()
    {
        var file = new File
        {
            Id = Guid.Parse("aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee"),
            Name = "photo.jpg",
            UserId = 1,
            Origin = FileOrigin.User,
            Attrs = new Dictionary<string, string> { ["projectid"] = "42" }
        };

        var path = _resolver.GetFullPath(file);

        Assert.Contains("user1", path);
        Assert.Contains("project42", path);
    }

    [Fact]
    public void GetFullPath_SystemOrigin_ReturnsSystemPath()
    {
        var file = new File
        {
            Id = Guid.Parse("aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee"),
            Name = "logo.png",
            Origin = FileOrigin.System
        };

        var path = _resolver.GetFullPath(file);

        Assert.StartsWith("system/", path);
        Assert.EndsWith(".png", path);
    }

    [Fact]
    public void GetFullPath_NoneOrigin_ReturnsNonePath()
    {
        var file = new File
        {
            Id = Guid.Parse("aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee"),
            Name = "data.bin",
            Origin = FileOrigin.None
        };

        var path = _resolver.GetFullPath(file);

        Assert.StartsWith("none/", path);
    }

    [Fact]
    public void GetFullPath_WithDimension_AppendsDimensionSuffix()
    {
        var fileId = Guid.Parse("aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee");
        var file = new File
        {
            Id = fileId,
            Name = "photo.jpg",
            Dim = 200,
            Origin = FileOrigin.System
        };

        var path = _resolver.GetFullPath(file);

        Assert.Contains("_200px", path);
        Assert.Contains(fileId.ToString(), path);
    }

    [Fact]
    public void GetFullPath_WithoutDimension_NoDimensionSuffix()
    {
        var fileId = Guid.Parse("aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee");
        var file = new File
        {
            Id = fileId,
            Name = "photo.jpg",
            Origin = FileOrigin.System
        };

        var path = _resolver.GetFullPath(file);

        Assert.DoesNotContain("px", path);
        Assert.Equal($"system/{fileId}.jpg", path);
    }

    [Fact]
    public void GetFullPath_WithFolder_IncludesFolderInPath()
    {
        var file = new File
        {
            Id = Guid.Parse("aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee"),
            Name = "doc.pdf",
            UserId = 1,
            Origin = FileOrigin.User,
            Attrs = new Dictionary<string, string> { ["folder"] = "documents" }
        };

        var path = _resolver.GetFullPath(file);

        Assert.Contains("documents", path);
    }

    [Fact]
    public void RemoveSpecialCharacters_RemovesNonAlphanumericCharacters()
    {
        var result = FilePathResolver.RemoveSpecialCharacters("hello@world#123!");

        Assert.Equal("helloworld123", result);
    }

    [Fact]
    public void RemoveSpecialCharacters_PreservesUnderscoreAndDash()
    {
        var result = FilePathResolver.RemoveSpecialCharacters("my-folder_name");

        Assert.Equal("my-folder_name", result);
    }

    [Fact]
    public void RemoveSpecialCharacters_NullOrEmpty_ReturnsEmpty()
    {
        Assert.Equal(string.Empty, FilePathResolver.RemoveSpecialCharacters(null));
        Assert.Equal(string.Empty, FilePathResolver.RemoveSpecialCharacters(""));
        Assert.Equal(string.Empty, FilePathResolver.RemoveSpecialCharacters("  "));
    }

    [Fact]
    public void GetResolutionPath_AppendsResolutionBeforeExtension()
    {
        var file = new File
        {
            Id = Guid.Parse("aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee"),
            Name = "photo.jpg",
            Origin = FileOrigin.System,
            Path = "system/aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee.jpg"
        };

        var path = _resolver.GetResolutionPath(file, 600);

        Assert.Equal("system/aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee_600.jpg", path);
    }

    [Fact]
    public void GetResolutionPath_WithDimInPath_AppendsResAfterDim()
    {
        var file = new File
        {
            Id = Guid.Parse("aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee"),
            Name = "photo.jpg",
            Dim = 200,
            Origin = FileOrigin.System,
            Path = "system/aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee_200px.jpg"
        };

        var path = _resolver.GetResolutionPath(file, 100);

        Assert.Equal("system/aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee_200px_100.jpg", path);
    }

    [Fact]
    public void GetResolutionPath_NoPathSet_UsesGetFullPath()
    {
        var file = new File
        {
            Id = Guid.Parse("aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee"),
            Name = "photo.jpg",
            Origin = FileOrigin.System
        };

        var path = _resolver.GetResolutionPath(file, 500);

        Assert.Contains("_500", path);
        Assert.EndsWith(".jpg", path);
    }
}
