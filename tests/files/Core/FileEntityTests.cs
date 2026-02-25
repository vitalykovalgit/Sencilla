namespace Sencilla.Component.Files.Tests;

/// <summary>
/// Tests for <see cref="File"/> entity.
///
/// Covers: entity interface compliance, default values, property assignment.
/// </summary>
public class FileEntityTests
{
    [Fact]
    public void File_ImplementsRequiredInterfaces()
    {
        var file = new File();

        Assert.IsAssignableFrom<IEntity<Guid>>(file);
        Assert.IsAssignableFrom<IEntityCreateable>(file);
        Assert.IsAssignableFrom<IEntityUpdateable>(file);
        Assert.IsAssignableFrom<IEntityRemoveable>(file);
        Assert.IsAssignableFrom<IEntityDeleteable>(file);
    }

    [Fact]
    public void File_DefaultOrigin_IsNone()
    {
        var file = new File();

        Assert.Equal(FileOrigin.None, file.Origin);
    }

    [Fact]
    public void File_AllProperties_CanBeSetAndRetrieved()
    {
        var id = Guid.NewGuid();
        var parentId = Guid.NewGuid();
        var now = DateTime.UtcNow;

        var file = new File
        {
            Id = id,
            ParentId = parentId,
            Name = "test.pdf",
            MimeType = "application/pdf",
            Size = 1024,
            Uploaded = 512,
            UserId = 42,
            Origin = FileOrigin.User,
            Path = "user42/test.pdf",
            Storage = 1,
            Dim = 100,
            Width = 800,
            Height = 600,
            CreatedDate = now,
            UpdatedDate = now,
            DeletedDate = now,
            Attrs = new Dictionary<string, string> { ["projectId"] = "1" }
        };

        Assert.Equal(id, file.Id);
        Assert.Equal(parentId, file.ParentId);
        Assert.Equal("test.pdf", file.Name);
        Assert.Equal("application/pdf", file.MimeType);
        Assert.Equal(1024, file.Size);
        Assert.Equal(512, file.Uploaded);
        Assert.Equal(42, file.UserId);
        Assert.Equal(FileOrigin.User, file.Origin);
        Assert.Equal("user42/test.pdf", file.Path);
        Assert.Equal(1, file.Storage);
        Assert.Equal(100, file.Dim);
        Assert.Equal(800, file.Width);
        Assert.Equal(600, file.Height);
        Assert.Equal(now, file.CreatedDate);
        Assert.Equal(now, file.UpdatedDate);
        Assert.Equal(now, file.DeletedDate);
        Assert.Equal("1", file.Attrs["projectId"]);
    }

    [Fact]
    public void File_Resolutions_DefaultIsNull()
    {
        var file = new File();

        Assert.Null(file.Resolutions);
    }

    [Fact]
    public void File_Resolutions_CanBeSetAndRetrieved()
    {
        var resolutions = new[] { 100, 200, 500 };
        var file = new File { Resolutions = resolutions };

        Assert.Equal(resolutions, file.Resolutions);
        Assert.Equal(3, file.Resolutions!.Length);
        Assert.Contains(200, file.Resolutions);
    }

    [Fact]
    public void File_Resolutions_EmptyArray()
    {
        var file = new File { Resolutions = [] };

        Assert.NotNull(file.Resolutions);
        Assert.Empty(file.Resolutions);
    }
}
