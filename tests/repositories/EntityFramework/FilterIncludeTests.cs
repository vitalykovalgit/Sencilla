using Microsoft.EntityFrameworkCore;

namespace Sencilla.Repository.EntityFramework.Tests;

public class IncludeParent : IEntity<int>
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public DateTime? When { get; set; }

    /// <summary>A collection of SCALARS — mapped, but not a navigation. This is IEntityTaggable.Tags' shape.</summary>
    public List<string>? Tags { get; set; }

    /// <summary>The only real navigation here.</summary>
    public ICollection<IncludeChild>? Children { get; set; }
}

public class IncludeChild : IEntity<int>
{
    public int Id { get; set; }
    public int IncludeParentId { get; set; }
    public string Label { get; set; } = string.Empty;
    public IncludeDetail? Detail { get; set; }
}

public class IncludeDetail : IEntity<int>
{
    public int Id { get; set; }
    public string Note { get; set; } = string.Empty;
}

public class IncludeDbContext(DbContextOptions<IncludeDbContext> options) : DbContext(options)
{
    public DbSet<IncludeParent> Parents { get; set; } = null!;
    public DbSet<IncludeChild> Children { get; set; } = null!;
    public DbSet<IncludeDetail> Details { get; set; } = null!;
}

/// <summary>
/// <c>?with=</c> is client input and <c>Include</c> rejects non-navigations at query-COMPILE time, not at the
/// <c>Include</c> call — so anything the handler lets through that isn't a navigation is a 500 raised by a
/// query string. These pin what it lets through.
/// </summary>
public class FilterIncludeTests : IDisposable
{
    readonly IncludeDbContext _context;

    public FilterIncludeTests()
    {
        _context = new IncludeDbContext(new DbContextOptionsBuilder<IncludeDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options);

        _context.Add(new IncludeParent
        {
            Id = 1,
            Name = "p",
            Children = [new IncludeChild { Id = 1, Label = "c", Detail = new IncludeDetail { Id = 1, Note = "n" } }],
        });
        _context.SaveChanges();
    }

    async Task<List<IncludeParent>> Read(params string[] with)
    {
        var @event = new EntityReadingEvent<IncludeParent>
        {
            Entities = _context.Set<IncludeParent>().AsNoTracking(),
            Filter = new Filter<IncludeParent> { With = with },
        };

        await new FilterConstraintHandler<IncludeParent>().HandleAsync(@event, default);

        return await @event.Entities!.ToListAsync();
    }

    [Theory]
    [InlineData("tags")]     // primitive collection — the case that motivated this
    [InlineData("name")]     // scalar
    [InlineData("when")]     // nullable value type
    [InlineData("id")]       // key
    [InlineData("bogus")]    // plain typo
    public async Task ANonNavigation_IsIgnoredRatherThanThrown(string with)
    {
        var rows = await Read(with);

        Assert.Single(rows);
        Assert.Null(rows[0].Children);
    }

    [Fact]
    public async Task ARealNavigation_IsStillIncluded_CaseInsensitively()
    {
        var rows = await Read("children");

        Assert.Single(Assert.Single(rows).Children!);
    }

    [Fact]
    public async Task APathThatBreaksHalfway_IncludesNothing()
    {
        // It used to fall back to the resolvable prefix, quietly loading a graph nobody asked for.
        var rows = await Read("children.bogus");

        Assert.Null(Assert.Single(rows).Children);
    }

    [Fact]
    public async Task APathThroughACollection_Resolves()
    {
        // Walking to the ELEMENT type is what makes a nested path past a collection resolvable at all.
        var child = Assert.Single(Assert.Single(await Read("children.detail")).Children!);

        Assert.NotNull(child.Detail);
    }

    [Fact]
    public async Task APathEndingOnAScalar_IncludesNothing()
    {
        // children.label is not an include path — Label is a column on the child, not a navigation.
        Assert.Null(Assert.Single(await Read("children.label")).Children);
    }

    public void Dispose() => _context.Dispose();
}
