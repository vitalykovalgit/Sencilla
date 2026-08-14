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
        var userId = Guid.NewGuid();
        var file = new File
        {
            Id = Guid.Parse("aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee"),
            Name = "photo.jpg",
            UserId = userId,
            Origin = FileOrigin.User
        };

        var path = _resolver.GetFullPath(file);

        Assert.Contains($"user{userId}", path);
        Assert.EndsWith(".jpg", path);
    }

    [Fact]
    public void GetFullPath_UserOrigin_WithProjectId_IncludesProjectDirectory()
    {
        var userId = Guid.NewGuid();
        var file = new File
        {
            Id = Guid.Parse("aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee"),
            Name = "photo.jpg",
            UserId = userId,
            Origin = FileOrigin.User,
            Attrs = new Dictionary<string, string> { ["projectid"] = "42" }
        };

        var path = _resolver.GetFullPath(file);

        Assert.Contains($"user{userId}", path);
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

    // Asserts a feature that no longer exists: the folder segment is commented out in
    // FilePathResolver.GetFullPath (`/* folderPath,*/`). Skipped rather than deleted so the decision to
    // drop folders from the path stays visible. Unrelated to the resolution-extension change.
    [Fact(Skip = "Folder segment is disabled in FilePathResolver.GetFullPath")]
    public void GetFullPath_WithFolder_IncludesFolderInPath()
    {
        var file = new File
        {
            Id = Guid.Parse("aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee"),
            Name = "doc.pdf",
            // Was `UserId = 1` — left behind by the int -> Guid primary key migration, so this file has not
            // compiled since. Unrelated to the resolution-extension change, fixed here to get the suite running.
            UserId = Guid.Parse("11111111-2222-3333-4444-555555555555"),
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

    // ── Derivative extension follows the derivative's OWN content type ───────────────────────
    // A 100px variant of a .jpg is re-encoded as WebP; naming it .jpg makes the stored file lie
    // to anything reading storage directly (a file browser, an S3 client, a support engineer).

    private static File JpegOriginalWith(params (string Res, string? Ct)[] variants)
    {
        var file = new File
        {
            Id = Guid.Parse("aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee"),
            Name = "photo.jpg",
            MimeType = "image/jpeg",
            Origin = FileOrigin.System,
            Path = "system/aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee.jpg",
            Res = new Dictionary<string, ResolutionInfo>()
        };

        foreach (var (res, ct) in variants)
            file.Res[res] = new ResolutionInfo { S = 1, U = 1, Ct = ct };

        return file;
    }

    [Fact]
    public void GetResolutionPath_WebpVariantOfJpegOriginal_UsesWebpExtension()
    {
        var file = JpegOriginalWith(("100", "image/webp"));

        Assert.EndsWith("_100.webp", _resolver.GetResolutionPath(file, 100));
    }

    [Fact]
    public void GetResolutionPath_JpegVariant_KeepsJpgExtension()
    {
        var file = JpegOriginalWith(("1600", "image/jpeg"));

        Assert.EndsWith("_1600.jpg", _resolver.GetResolutionPath(file, 1600));
    }

    [Fact]
    public void GetResolutionPath_MixedVariants_EachFollowsItsOwnType()
    {
        var file = JpegOriginalWith(("100", "image/webp"), ("1600", "image/jpeg"));

        Assert.EndsWith("_100.webp", _resolver.GetResolutionPath(file, 100));
        Assert.EndsWith("_1600.jpg", _resolver.GetResolutionPath(file, 1600));
    }

    [Fact]
    public void GetResolutionPath_ExplicitContentType_WinsOverRecordedOne()
    {
        // The create path: the variant is being written, so Res[key].Ct is not stored yet (or is stale).
        var file = JpegOriginalWith(("100", "image/jpeg"));

        Assert.EndsWith("_100.webp", _resolver.GetResolutionPath(file, 100, "image/webp"));
    }

    [Fact]
    public void GetResolutionPath_NoRecordedTypeAndNoneGiven_KeepsOriginalExtension()
    {
        var file = JpegOriginalWith(("100", null));

        Assert.EndsWith("_100.jpg", _resolver.GetResolutionPath(file, 100));
    }

    [Fact]
    public void GetResolutionPath_UnknownContentType_KeepsOriginalExtension()
    {
        var file = JpegOriginalWith(("100", "application/octet-stream"));

        Assert.EndsWith("_100.jpg", _resolver.GetResolutionPath(file, 100));
    }

    [Theory]
    [InlineData("image/webp", ".webp")]
    [InlineData("IMAGE/WEBP", ".webp")]
    [InlineData("  image/webp  ", ".webp")]
    [InlineData("image/webp; charset=binary", ".webp")]
    [InlineData("image/png", ".png")]
    [InlineData("image/avif", ".avif")]
    [InlineData("image/svg+xml", ".svg")]
    public void GetResolutionPath_ContentTypeVariants_MapToExpectedExtension(string contentType, string expected)
    {
        var path = _resolver.GetResolutionPath("system/photo.jpg", "100", contentType);

        Assert.Equal($"system/photo_100{expected}", path);
    }

    [Fact]
    public void GetResolutionPath_StringOverloadWithoutContentType_KeepsOriginalExtension()
    {
        Assert.Equal("system/photo_100.jpg", _resolver.GetResolutionPath("system/photo.jpg", "100"));
    }

    [Fact]
    public void GetResolutionPath_WritePathAndReadPathAgree()
    {
        // The invariant that matters: whoever CREATES the variant passes the content type explicitly,
        // and every later reader resolves the same name from the recorded Res entry. If these two ever
        // diverge, every thumbnail 404s.
        var original = JpegOriginalWith();
        var writePath = _resolver.GetResolutionPath(original, 100, "image/webp");

        var afterUpload = JpegOriginalWith(("100", "image/webp"));
        var readPath = _resolver.GetResolutionPath(afterUpload, 100);

        Assert.Equal(writePath, readPath);
    }
}
