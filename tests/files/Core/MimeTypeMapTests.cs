namespace Sencilla.Component.Files.Tests;

/// <summary>
/// Tests for <see cref="MimeTypeMap"/>.
///
/// Covers: GetMimeType, GetExtension, common file type lookups.
/// </summary>
public class MimeTypeMapTests
{
    // ── GetMimeType ──────────────────────────────────────────────────────────

    [Theory]
    [InlineData("photo.jpg", "image/jpeg")]
    [InlineData("photo.jpeg", "image/jpeg")]
    [InlineData("document.pdf", "application/pdf")]
    [InlineData("page.html", "text/html")]
    [InlineData("style.css", "text/css")]
    [InlineData("script.js", "application/javascript")]
    [InlineData("data.json", "application/json")]
    [InlineData("archive.zip", "application/zip")]
    [InlineData("image.png", "image/png")]
    [InlineData("image.gif", "image/gif")]
    [InlineData("video.mp4", "video/mp4")]
    [InlineData("song.mp3", "audio/mpeg")]
    [InlineData("doc.docx", "application/vnd.openxmlformats-officedocument.wordprocessingml.document")]
    [InlineData("sheet.xlsx", "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet")]
    public void GetMimeType_KnownExtension_ReturnsCorrectMimeType(string fileName, string expectedMime)
    {
        var result = MimeTypeMap.GetMimeType(fileName);

        Assert.Equal(expectedMime, result);
    }

    [Fact]
    public void GetMimeType_UnknownExtension_ReturnsOctetStream()
    {
        var result = MimeTypeMap.GetMimeType("file.xyz123unknown");

        Assert.Equal("application/octet-stream", result);
    }

    [Fact]
    public void GetMimeType_CaseInsensitive_ReturnsCorrectMimeType()
    {
        var lower = MimeTypeMap.GetMimeType("file.jpg");
        var upper = MimeTypeMap.GetMimeType("file.JPG");
        var mixed = MimeTypeMap.GetMimeType("file.JpG");

        Assert.Equal(lower, upper);
        Assert.Equal(lower, mixed);
    }

    [Fact]
    public void GetMimeType_FileNameWithPath_ReturnsCorrectMimeType()
    {
        var result = MimeTypeMap.GetMimeType("/path/to/file.pdf");

        Assert.Equal("application/pdf", result);
    }

    // ── GetExtension ─────────────────────────────────────────────────────────

    [Theory]
    [InlineData("image/jpeg", ".jpg")]
    [InlineData("application/pdf", ".pdf")]
    [InlineData("text/html", ".html")]
    [InlineData("application/json", ".json")]
    [InlineData("image/png", ".png")]
    public void GetExtension_KnownMimeType_ReturnsCorrectExtension(string mimeType, string expectedExt)
    {
        var result = MimeTypeMap.GetExtension(mimeType);

        Assert.Equal(expectedExt, result);
    }
}
