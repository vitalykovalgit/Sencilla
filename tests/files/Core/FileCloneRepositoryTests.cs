using System.Linq.Expressions;
using Microsoft.Extensions.Logging.Abstractions;

namespace Sencilla.Component.Files.Tests;

/// <summary>
/// Tests for <see cref="FileCloneRepository"/>. Uses the REAL <see cref="FilePathResolver"/> — the whole
/// point of cloning through the typed entity is that resolution paths resolve at each variant's
/// own content type, and a mocked resolver would assert nothing about that.
/// </summary>
public class FileCloneRepositoryTests
{
    private static readonly Guid SourceUser = Guid.Parse("11111111-1111-1111-1111-111111111111");
    private static readonly Guid TargetUser = Guid.Parse("22222222-2222-2222-2222-222222222222");
    private const string SourceProject = "aaaa1111";
    private const string TargetProject = "bbbb2222";

    private readonly Mock<IFileStorage> _storage = new();
    private readonly Mock<IReadRepository<File, Guid>> _files = new();
    private readonly Mock<ICreateRepository<File, Guid>> _create = new();
    private readonly IFilePathResolver _paths = new FilePathResolver();

    private readonly List<File> _seed = [];
    private readonly HashSet<string> _existingBlobs = [];
    private readonly List<string> _written = [];
    private readonly List<File> _created = [];

    private FileCloneRepository Cloner()
    {
        _files.Setup(r => r.GetAll(It.IsAny<IFilter>(), It.IsAny<CancellationToken>(), It.IsAny<Expression<Func<File, object>>[]>()))
            .ReturnsAsync((IFilter f, CancellationToken _, Expression<Func<File, object>>[] __) => Match(f));

        _storage.Setup(s => s.ReadFileAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((string path, CancellationToken _) =>
                _existingBlobs.Contains(path) ? new MemoryStream([1, 2, 3]) : null);

        _storage.Setup(s => s.SaveFile(It.IsAny<string>(), It.IsAny<Stream>()))
            .ReturnsAsync(true)
            .Callback((string path, Stream _) => _written.Add(path));

        _create.Setup(r => r.Create(It.IsAny<File>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((File f, CancellationToken _) => f)
            .Callback((File f, CancellationToken _) => _created.Add(f));

        return new FileCloneRepository(_storage.Object, _paths, NullLogger<FileCloneRepository>.Instance, _files.Object, _create.Object);
    }

    /// <summary>Stands in for the repository's filter handling: the only shapes the cloner sends.</summary>
    private IEnumerable<File> Match(IFilter filter)
    {
        var ids = filter.Properties!.TryGetValue(nameof(File.Id), out var byId)
            ? byId.Values!.Cast<Guid>().ToHashSet() : null;
        if (ids != null)
            return _seed.Where(f => ids.Contains(f.Id)).ToList();

        var parents = filter.Properties[nameof(File.ParentId)].Values!.Cast<Guid>().ToHashSet();
        return _seed.Where(f => f.ParentId != null && parents.Contains(f.ParentId.Value)).ToList();
    }

    private File Seed(string name, Guid? parentId = null, IDictionary<string, ResolutionInfo>? res = null)
    {
        var file = new File
        {
            Id = Guid.NewGuid(),
            ParentId = parentId,
            Name = name,
            MimeType = "image/jpeg",
            Size = 2048,
            UserId = SourceUser,
            Origin = FileOrigin.User,
            Storage = 2,
            Res = res,
            Attrs = new Dictionary<string, string> { ["projectid"] = SourceProject, ["folder"] = "keep-me" },
        };
        file.Path = _paths.GetFullPath(file);

        _seed.Add(file);
        _existingBlobs.Add(file.Path);
        return file;
    }

    private static IDictionary<string, string> ToTargetProject() => new Dictionary<string, string> { ["projectId"] = TargetProject };

    [Fact]
    public async Task Clone_WritesRowAndBlob_UnderTheNewOwnerAndScope()
    {
        var source = Seed("photo.jpg");

        var map = await Cloner().Clone([source.Id], TargetUser, ToTargetProject());

        var clone = Assert.Single(_created);
        Assert.Equal(map[source.Id], clone.Id);
        Assert.Equal(TargetUser, clone.UserId);
        Assert.Equal($"user{TargetUser}/project{TargetProject}/{clone.Id}.jpg", clone.Path);
        Assert.Contains(clone.Path, _written);
    }

    [Fact]
    public async Task Clone_CopiesResolutionVariants_AtTheirOwnContentType()
    {
        // A WebP derivative of a JPEG original: Ct is what makes its path _600.webp, and reading it
        // is only possible for a caller that can see the typed Res.
        var source = Seed("photo.jpg", res: new Dictionary<string, ResolutionInfo>
        {
            ["600"] = new ResolutionInfo { S = 900, U = 900, Ct = "image/webp" },
        });
        _existingBlobs.Add($"user{SourceUser}/project{SourceProject}/{source.Id}_600.webp");

        var map = await Cloner().Clone([source.Id], TargetUser, ToTargetProject());

        var newId = map[source.Id];
        Assert.Contains($"user{TargetUser}/project{TargetProject}/{newId}_600.webp", _written);
        Assert.DoesNotContain($"user{TargetUser}/project{TargetProject}/{newId}_600.jpg", _written);
    }

    [Fact]
    public async Task Clone_BringsChildrenAlong_AndRepointsThemAtTheClonedParent()
    {
        var parent = Seed("photo.jpg");
        var child = Seed("photo.jpg", parentId: parent.Id);

        var map = await Cloner().Clone([parent.Id], TargetUser, ToTargetProject());

        Assert.Equal(2, _created.Count);
        Assert.Contains(child.Id, map.Keys);

        var clonedChild = _created.Single(f => f.Id == map[child.Id]);
        Assert.Equal(map[parent.Id], clonedChild.ParentId);

        // Parent row is written before the child that references it — ParentId is a self-FK.
        Assert.True(_created.FindIndex(f => f.Id == map[parent.Id]) < _created.FindIndex(f => f.Id == map[child.Id]));
    }

    [Fact]
    public async Task Clone_MergesAttrsOverTheSource_LeavingOthersIntact()
    {
        var source = Seed("photo.jpg");

        await Cloner().Clone([source.Id], TargetUser, ToTargetProject());

        var clone = Assert.Single(_created);
        Assert.Equal(TargetProject, clone.Attrs!["projectid"]);
        Assert.False(clone.Attrs.ContainsKey("projectId"));
        Assert.Equal("keep-me", clone.Attrs["folder"]);
        Assert.Equal(SourceProject, source.Attrs!["projectid"]); // the source is not touched
    }

    [Fact]
    public async Task Clone_MissingSourceBlob_StillWritesTheRow()
    {
        // The clone mirrors the source's brokenness instead of failing forever.
        var source = Seed("photo.jpg");
        _existingBlobs.Clear();

        var map = await Cloner().Clone([source.Id], TargetUser, ToTargetProject());

        Assert.Single(_created);
        Assert.Empty(_written);
        Assert.Single(map);
    }

    [Fact]
    public async Task Clone_RequestedIdThatIsAlsoAChild_IsClonedOnce()
    {
        var parent = Seed("photo.jpg");
        var child = Seed("photo.jpg", parentId: parent.Id);

        var map = await Cloner().Clone([parent.Id, child.Id], TargetUser, ToTargetProject());

        Assert.Equal(2, _created.Count);
        Assert.Equal(2, map.Count);
    }

    [Fact]
    public async Task Clone_NoIds_TouchesNothing()
    {
        var map = await Cloner().Clone([], TargetUser, ToTargetProject());

        Assert.Empty(map);
        Assert.Empty(_created);
        _files.Verify(r => r.GetAll(It.IsAny<IFilter>(), It.IsAny<CancellationToken>(), It.IsAny<Expression<Func<File, object>>[]>()), Times.Never);
    }
}
