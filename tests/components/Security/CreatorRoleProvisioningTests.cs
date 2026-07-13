using Moq;
using Sencilla.Core;
using Sencilla.Component.Users;

namespace Sencilla.Component.Security.Tests;

public class CreatorRoleProvisioningTests
{
    [SecureWith(typeof(ProjectUser))]
    public class Project : IEntity<int>
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
    }

    public class ProjectUser : IEntity<Guid>, IEntityRoleable<int>
    {
        public Guid Id { get; set; }
        public Guid UserId { get; set; }
        public int EntityId { get; set; }
        public int RoleId { get; set; }
    }

    [SecureWith(typeof(TeamUser), CreatorRole = (int)RoleType.Editor)]
    public class Team : IEntity<int>
    {
        public int Id { get; set; }
    }

    public class TeamUser : IEntity<Guid>, IEntityRoleable<int>
    {
        public Guid Id { get; set; }
        public Guid UserId { get; set; }
        public int EntityId { get; set; }
        public int RoleId { get; set; }
    }

    private readonly CreatorRoleProvisioningHandler<Project, int, ProjectUser, Guid> _handler = new();
    private readonly Mock<ICreateRepository<ProjectUser, Guid>> _linkRepo = new();
    private readonly List<ProjectUser> _createdLinks = [];

    public CreatorRoleProvisioningTests()
    {
        _linkRepo
            .Setup(r => r.Create(It.IsAny<IEnumerable<ProjectUser>>(), It.IsAny<CancellationToken>()))
            .Callback<IEnumerable<ProjectUser>, CancellationToken>((links, _) => _createdLinks.AddRange(links))
            .ReturnsAsync((IEnumerable<ProjectUser> links, CancellationToken _) => links);
    }

    private static ISystemVariable SysVarsFor(User? user) =>
        Mock.Of<ISystemVariable>(s => s.Get<User>("user") == user);

    [Fact]
    public async Task CreatedEvent_ProvisionsCreatorRoleRow_DefaultOwner()
    {
        var user = new User { Id = Guid.NewGuid(), Email = "u@example.com" };
        var e = new EntityCreatedEvent<Project> { Entities = new[] { new Project { Id = 42 } }.AsQueryable() };

        await _handler.HandleAsync(e, SysVarsFor(user), _linkRepo.Object, CancellationToken.None);

        var link = Assert.Single(_createdLinks);
        Assert.Equal(user.Id, link.UserId);
        Assert.Equal(42, link.EntityId);
        Assert.Equal((int)RoleType.Owner, link.RoleId);
    }

    [Fact]
    public async Task CreatedEvent_RespectsCustomCreatorRole()
    {
        var handler = new CreatorRoleProvisioningHandler<Team, int, TeamUser, Guid>();
        var repo = new Mock<ICreateRepository<TeamUser, Guid>>();
        var created = new List<TeamUser>();
        repo.Setup(r => r.Create(It.IsAny<IEnumerable<TeamUser>>(), It.IsAny<CancellationToken>()))
            .Callback<IEnumerable<TeamUser>, CancellationToken>((links, _) => created.AddRange(links))
            .ReturnsAsync((IEnumerable<TeamUser> links, CancellationToken _) => links);

        var user = new User { Id = Guid.NewGuid(), Email = "u@example.com" };
        var e = new EntityCreatedEvent<Team> { Entities = new[] { new Team { Id = 7 } }.AsQueryable() };

        await handler.HandleAsync(e, SysVarsFor(user), repo.Object, CancellationToken.None);

        Assert.Equal((int)RoleType.Editor, Assert.Single(created).RoleId);
    }

    [Fact]
    public async Task UnhydratedKey_ThrowsRatherThanMisprovision()
    {
        // Id still default here = the publisher did not read the DB-generated key back
        // (raw Upsert/Merge bulk). Provisioning against EntityId = 0 would grant a role
        // on a non-existent/unrelated row, so fail loudly instead.
        var user = new User { Id = Guid.NewGuid(), Email = "u@example.com" };
        var e = new EntityCreatedEvent<Project> { Entities = new[] { new Project { Id = 0 } }.AsQueryable() };

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _handler.HandleAsync(e, SysVarsFor(user), _linkRepo.Object, CancellationToken.None));
        Assert.Empty(_createdLinks);
    }

    [Fact]
    public async Task AnonymousCreate_ProvisionsNothing()
    {
        var e = new EntityCreatedEvent<Project> { Entities = new[] { new Project { Id = 42 } }.AsQueryable() };

        await _handler.HandleAsync(e, SysVarsFor(null), _linkRepo.Object, CancellationToken.None);

        Assert.Empty(_createdLinks);
    }

    [Fact]
    public async Task BulkCreate_ProvisionsOneLinkPerEntity()
    {
        var user = new User { Id = Guid.NewGuid(), Email = "u@example.com" };
        var e = new EntityCreatedEvent<Project> { Entities = new[] { new Project { Id = 1 }, new Project { Id = 2 } }.AsQueryable() };

        await _handler.HandleAsync(e, SysVarsFor(user), _linkRepo.Object, CancellationToken.None);

        Assert.Equal(new[] { 1, 2 }, _createdLinks.Select(l => l.EntityId).Order().ToArray());
    }
}
