using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Sencilla.Core;
using Sencilla.Component.Users;

namespace Sencilla.Component.Security.Tests;

public class SecureWithFilterTests
{
    // ── Model: Project ← ProjectItem ← ProjectFile, roles Owner(6) ⊃ Editor(7) ⊃ Viewer(8) ──

    [SecureWith(typeof(ProjectUser))]
    public class Project : IEntity<Guid> { public Guid Id { get; set; } }

    public class ProjectUser : IEntity<Guid>, IEntityRoleable<Guid>
    {
        public Guid Id { get; set; }
        public Guid UserId { get; set; }
        public Guid EntityId { get; set; }
        public int RoleId { get; set; }
    }

    [SecureWith(typeof(Project), Via = nameof(ProjectId))]
    public class ProjectItem : IEntity<Guid>
    {
        public Guid Id { get; set; }
        public Guid ProjectId { get; set; }
    }

    [SecureWith(typeof(ProjectItem), Via = nameof(ItemId))]
    public class ProjectFile : IEntity<Guid>
    {
        public Guid Id { get; set; }
        public Guid ItemId { get; set; }
    }

    public class Unsecured : IEntity<Guid> { public Guid Id { get; set; } }

    private static readonly Guid Me = Guid.NewGuid();
    private static readonly Guid Stranger = Guid.NewGuid();

    private readonly Project _projectA = new() { Id = Guid.NewGuid() };
    private readonly Project _projectB = new() { Id = Guid.NewGuid() };
    private readonly ProjectItem _itemInA;
    private readonly ProjectItem _itemInB;
    private readonly List<ProjectUser> _links = [];
    private readonly IServiceProvider _services;
    private readonly IRoleClosure _closure;

    public SecureWithFilterTests()
    {
        _itemInA = new ProjectItem { Id = Guid.NewGuid(), ProjectId = _projectA.Id };
        _itemInB = new ProjectItem { Id = Guid.NewGuid(), ProjectId = _projectB.Id };

        var services = new ServiceCollection();
        services.AddSingleton(Mock.Of<IReadRepository<ProjectUser, Guid>>(r => r.Query == _links.AsQueryable()));
        services.AddSingleton(Mock.Of<IReadRepository<ProjectItem, Guid>>(r => r.Query == new[] { _itemInA, _itemInB }.AsQueryable()));
        _services = services.BuildServiceProvider();

        var roles = new Mock<IReadRepository<Role, int>>();
        roles.Setup(r => r.Query).Returns(new[]
        {
            new Role { Id = 6, Name = "Owner", ParentId = 7 },
            new Role { Id = 7, Name = "Editor", ParentId = 8 },
            new Role { Id = 8, Name = "Viewer" },
        }.AsQueryable());
        _closure = new RoleClosure(roles.Object, new MemoryCache(new MemoryCacheOptions()), new SecurityCacheSignal());
    }

    private void Hold(Guid userId, Guid entityId, RoleType role) =>
        _links.Add(new ProjectUser { Id = Guid.NewGuid(), UserId = userId, EntityId = entityId, RoleId = (int)role });

    private bool Evaluate<T>(int matrixRole, T row) =>
        SecureWithFilter.Build<T>(matrixRole, Me, _services, _closure)!.Compile()(row);

    [Fact]
    public void Terminal_MatchesOwnHolding()
    {
        Hold(Me, _projectA.Id, RoleType.Viewer);

        Assert.True(Evaluate((int)RoleType.Viewer, _projectA));
        Assert.False(Evaluate((int)RoleType.Viewer, _projectB));
    }

    [Fact]
    public void InheritedHolding_ActivatesAncestorRow()
    {
        // Owner ⊃ Editor ⊃ Viewer: holding Owner satisfies a Viewer matrix row.
        Hold(Me, _projectA.Id, RoleType.Owner);

        Assert.True(Evaluate((int)RoleType.Viewer, _projectA));
        Assert.True(Evaluate((int)RoleType.Editor, _projectA));
    }

    [Fact]
    public void LowerHolding_DoesNotActivateHigherRow()
    {
        Hold(Me, _projectA.Id, RoleType.Viewer);

        Assert.False(Evaluate((int)RoleType.Editor, _projectA));
    }

    [Fact]
    public void OneHop_ChecksHoldingOnParent()
    {
        Hold(Me, _projectA.Id, RoleType.Editor);

        Assert.True(Evaluate((int)RoleType.Editor, _itemInA));
        Assert.False(Evaluate((int)RoleType.Editor, _itemInB));
    }

    [Fact]
    public void TwoHops_ChecksHoldingOnGrandparent()
    {
        Hold(Me, _projectA.Id, RoleType.Editor);
        var fileInA = new ProjectFile { Id = Guid.NewGuid(), ItemId = _itemInA.Id };
        var fileInB = new ProjectFile { Id = Guid.NewGuid(), ItemId = _itemInB.Id };

        Assert.True(Evaluate((int)RoleType.Editor, fileInA));
        Assert.False(Evaluate((int)RoleType.Editor, fileInB));
    }

    [Fact]
    public void OtherUsersHolding_DoesNotActivate()
    {
        Hold(Stranger, _projectA.Id, RoleType.Owner);

        Assert.False(Evaluate((int)RoleType.Viewer, _projectA));
    }

    [Fact]
    public void UndeclaredEntity_ReturnsNull()
    {
        Assert.Null(SecureWithFilter.Build<Unsecured>((int)RoleType.Viewer, Me, _services, _closure));
    }
}
