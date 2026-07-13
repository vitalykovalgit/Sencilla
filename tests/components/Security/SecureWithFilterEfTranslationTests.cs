using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Sencilla.Core;
using Sencilla.Component.Users;

namespace Sencilla.Component.Security.Tests;

/// <summary>
/// End-to-end EF translation guard for <see cref="SecureWithFilter"/>. The pure-unit
/// tests evaluate predicates via .Compile() (LINQ-to-Objects), which happily runs a
/// tree EF Core can never translate — so they cannot catch a broken query root. These
/// tests run the built predicate through a real Sqlite DbContext (.Where(pred).ToList()),
/// forcing translation to SQL. If QueryOf ever regresses to Expression.Constant(DbSet)
/// instead of the queryable's own root expression, every case here throws
/// "could not be translated".
/// </summary>
public class SecureWithFilterEfTranslationTests : IDisposable
{
    [SecureWith(typeof(ProjectUser))]
    public class Project : IEntity<Guid> { public Guid Id { get; set; } }

    // Self-securing role table: secures itself (in hop mode) through the Project it
    // grants a role on — sharing management ("only Owners add members") on the same machinery.
    [SecureWith(typeof(Project), Via = nameof(EntityId))]
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

    private class Db(DbContextOptions<Db> options) : DbContext(options)
    {
        public DbSet<Project> Projects => Set<Project>();
        public DbSet<ProjectUser> ProjectUsers => Set<ProjectUser>();
        public DbSet<ProjectItem> ProjectItems => Set<ProjectItem>();
    }

    private readonly SqliteConnection _connection;
    private readonly Db _db;
    private readonly IServiceProvider _services;
    private readonly IRoleClosure _closure;

    private static readonly Guid Me = Guid.NewGuid();
    private static readonly Guid Stranger = Guid.NewGuid();

    public SecureWithFilterEfTranslationTests()
    {
        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();
        _db = new Db(new DbContextOptionsBuilder<Db>().UseSqlite(_connection).Options);
        _db.Database.EnsureCreated();

        // Repositories whose raw Query is the live DbSet — SecureWithFilter roots its
        // EXISTS subquery here, and it must translate within the one DbContext.
        var sc = new ServiceCollection();
        sc.AddSingleton(Mock.Of<IReadRepository<ProjectUser, Guid>>(r => r.Query == _db.ProjectUsers));
        sc.AddSingleton(Mock.Of<IReadRepository<ProjectItem, Guid>>(r => r.Query == _db.ProjectItems));
        _services = sc.BuildServiceProvider();

        var roles = new Mock<IReadRepository<Role, int>>();
        roles.Setup(r => r.Query).Returns(new[]
        {
            new Role { Id = 6, Name = "Owner", ParentId = 7 },
            new Role { Id = 7, Name = "Editor", ParentId = 8 },
            new Role { Id = 8, Name = "Viewer" },
        }.AsQueryable());
        _closure = new RoleClosure(roles.Object, new MemoryCache(new MemoryCacheOptions()), new SecurityCacheSignal());
    }

    private Guid SeedProject(Guid owner, RoleType role)
    {
        var p = new Project { Id = Guid.NewGuid() };
        _db.Projects.Add(p);
        _db.ProjectUsers.Add(new ProjectUser { Id = Guid.NewGuid(), UserId = owner, EntityId = p.Id, RoleId = (int)role });
        _db.SaveChanges();
        return p.Id;
    }

    [Fact]
    public void Terminal_TranslatesAndFiltersToHeldRows()
    {
        var mine = SeedProject(Me, RoleType.Viewer);
        var theirs = SeedProject(Stranger, RoleType.Owner);

        var pred = SecureWithFilter.Build<Project>((int)RoleType.Viewer, Me, _services, _closure)!;
        var visible = _db.Projects.Where(pred).Select(p => p.Id).ToList(); // <- forces SQL translation

        Assert.Contains(mine, visible);
        Assert.DoesNotContain(theirs, visible);
    }

    [Fact]
    public void Terminal_InheritedHolding_TranslatesAndActivatesAncestorRow()
    {
        var mine = SeedProject(Me, RoleType.Owner); // Owner ⊃ Viewer

        var pred = SecureWithFilter.Build<Project>((int)RoleType.Viewer, Me, _services, _closure)!;
        Assert.Contains(mine, _db.Projects.Where(pred).Select(p => p.Id).ToList());
    }

    [Fact]
    public void OneHop_TranslatesCorrelatedSubqueryOnParentHolding()
    {
        var mine = SeedProject(Me, RoleType.Editor);
        var theirs = SeedProject(Stranger, RoleType.Editor);
        _db.ProjectItems.Add(new ProjectItem { Id = Guid.NewGuid(), ProjectId = mine });
        var theirItem = new ProjectItem { Id = Guid.NewGuid(), ProjectId = theirs };
        _db.ProjectItems.Add(theirItem);
        _db.SaveChanges();

        var pred = SecureWithFilter.Build<ProjectItem>((int)RoleType.Editor, Me, _services, _closure)!;
        var visible = _db.ProjectItems.Where(pred).Select(i => i.ProjectId).ToList();

        Assert.Contains(mine, visible);
        Assert.DoesNotContain(theirs, visible);
    }

    [Fact]
    public void SelfSecuringCreate_ExcludesInsertedRow_SoItCannotSelfAuthorize()
    {
        // The escalation: an attacker holding NOTHING inserts an Owner row on a victim
        // project. The post-image held-Create check must NOT accept that very row as the
        // proof of holding. Simulate the check against the just-inserted row.
        var victim = new Project { Id = Guid.NewGuid() };
        _db.Projects.Add(victim);
        var forged = new ProjectUser { Id = Guid.NewGuid(), UserId = Me, EntityId = victim.Id, RoleId = (int)RoleType.Owner };
        _db.ProjectUsers.Add(forged);
        _db.SaveChanges();

        // Excluding the inserted row, the EXISTS finds no PRE-existing holding → filtered out.
        var guarded = SecureWithFilter.Build<ProjectUser>((int)RoleType.Owner, Me, _services, _closure,
            excludeSelfKeys: new object[] { forged.Id })!;
        Assert.Empty(_db.ProjectUsers.Where(guarded).Where(u => u.Id == forged.Id).ToList());

        // Without the exclusion the row self-authorizes (the bug) — proves the guard is load-bearing.
        var unguarded = SecureWithFilter.Build<ProjectUser>((int)RoleType.Owner, Me, _services, _closure)!;
        Assert.Single(_db.ProjectUsers.Where(unguarded).Where(u => u.Id == forged.Id).ToList());
    }

    [Fact]
    public void SelfSecuringCreate_PreExistingHolding_AuthorizesTheNewRow()
    {
        // Legitimate sharing: an Owner of a project adds a member. The Owner's PRE-existing
        // row (not excluded) authorizes the newly inserted member row.
        var project = SeedProject(Me, RoleType.Owner);
        var newMember = new ProjectUser { Id = Guid.NewGuid(), UserId = Stranger, EntityId = project, RoleId = (int)RoleType.Viewer };
        _db.ProjectUsers.Add(newMember);
        _db.SaveChanges();

        var guarded = SecureWithFilter.Build<ProjectUser>((int)RoleType.Owner, Me, _services, _closure,
            excludeSelfKeys: new object[] { newMember.Id })!;
        Assert.Single(_db.ProjectUsers.Where(guarded).Where(u => u.Id == newMember.Id).ToList());
    }

    public void Dispose()
    {
        _db.Dispose();
        _connection.Dispose();
    }
}
