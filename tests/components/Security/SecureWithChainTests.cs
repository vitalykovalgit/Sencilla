using Sencilla.Core;

namespace Sencilla.Component.Security.Tests;

public class SecureWithChainTests
{
    // ── Happy-path model: Project ← ProjectItem ← ProjectFile, self-securing role table ──

    [SecureWith(typeof(ProjectUser))]
    public class Project : IEntity<Guid> { public Guid Id { get; set; } }

    [SecureWith(typeof(Project), Via = nameof(EntityId))] // role table secures ITSELF via its project
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

    [Fact]
    public void Terminal_ResolvesWithNoHops()
    {
        var chain = SecureWithChains.TryResolve(typeof(Project))!;

        Assert.Equal(typeof(Project), chain.Terminal);
        Assert.Equal(typeof(ProjectUser), chain.LinkType);
        Assert.Equal(typeof(Guid), chain.TerminalKeyType);
        Assert.Equal((int)6, chain.CreatorRole);
        Assert.Empty(chain.Hops);
    }

    [Fact]
    public void OneHop_ResolvesThroughParent()
    {
        var chain = SecureWithChains.TryResolve(typeof(ProjectItem))!;

        Assert.Equal(typeof(Project), chain.Terminal);
        Assert.Equal(typeof(ProjectUser), chain.LinkType);
        var hop = Assert.Single(chain.Hops);
        Assert.Equal(typeof(ProjectItem), hop.Entity);
        Assert.Equal(nameof(ProjectItem.ProjectId), hop.Via.Name);
    }

    [Fact]
    public void TwoHops_ComposeTransitively()
    {
        var chain = SecureWithChains.TryResolve(typeof(ProjectFile))!;

        Assert.Equal(typeof(Project), chain.Terminal);
        Assert.Equal(2, chain.Hops.Count);
        Assert.Equal(nameof(ProjectFile.ItemId), chain.Hops[0].Via.Name);
        Assert.Equal(nameof(ProjectItem.ProjectId), chain.Hops[1].Via.Name);
    }

    [Fact]
    public void RoleTable_SecuresItself_ThroughItsEntity()
    {
        // Sharing management: "only Owners may add members" — same machinery.
        var chain = SecureWithChains.TryResolve(typeof(ProjectUser))!;

        Assert.Equal(typeof(Project), chain.Terminal);
        Assert.Equal(typeof(ProjectUser), chain.LinkType);
        var hop = Assert.Single(chain.Hops);
        Assert.Equal(nameof(ProjectUser.EntityId), hop.Via.Name);
    }

    [Fact]
    public void Undeclared_ResolvesToNull()
    {
        Assert.Null(SecureWithChains.TryResolve(typeof(Unsecured)));
    }

    // ── Misdeclarations fail loudly ──────────────────────────────────────────

    [SecureWith(typeof(Project))] // hop source without Via
    public class HopWithoutVia : IEntity<Guid> { public Guid Id { get; set; } }

    [SecureWith(typeof(ProjectUser), Via = nameof(ProjectId))] // terminal with Via
    public class TerminalWithVia : IEntity<Guid> { public Guid Id { get; set; } public Guid ProjectId { get; set; } }

    [SecureWith(typeof(Project), Via = "NoSuchProperty")]
    public class MissingViaProperty : IEntity<Guid> { public Guid Id { get; set; } }

    [SecureWith(typeof(Project), Via = nameof(ProjectNumber))] // int FK vs Guid key
    public class ViaTypeMismatch : IEntity<Guid> { public Guid Id { get; set; } public int ProjectNumber { get; set; } }

    [SecureWith(typeof(ProjectUser))] // ProjectUser is IEntityRoleable<Guid>, entity keyed by int
    public class KeyMismatch : IEntity<int> { public int Id { get; set; } }

    [SecureWith(typeof(CycleB), Via = nameof(BId))]
    public class CycleA : IEntity<Guid> { public Guid Id { get; set; } public Guid BId { get; set; } }

    [SecureWith(typeof(CycleA), Via = nameof(AId))]
    public class CycleB : IEntity<Guid> { public Guid Id { get; set; } public Guid AId { get; set; } }

    [SecureWith(typeof(Unsecured), Via = nameof(TargetId))] // parent has no [SecureWith]
    public class NonTerminating : IEntity<Guid> { public Guid Id { get; set; } public Guid TargetId { get; set; } }

    [SecureWith(typeof(SelfTerminal))] // an entity as its own role table -> provisioning would recurse
    public class SelfTerminal : IEntity<Guid>, IEntityRoleable<Guid>
    {
        public Guid Id { get; set; }
        public Guid UserId { get; set; }
        public Guid EntityId { get; set; }
        public int RoleId { get; set; }
    }

    [SecureWith(typeof(ProjectUser), CreatorRole = 3 /* User — claims-derived, global-only */)]
    public class ClaimsDerivedCreator : IEntity<Guid> { public Guid Id { get; set; } }

    public class ExplicitLink : IEntity<Guid>, IEntityRoleable<Guid>
    {
        public Guid Id { get; set; }
        Guid IEntityRoleable.UserId { get; set; }        // explicit — no public UserId
        int IEntityRoleable.RoleId { get; set; }
        Guid IEntityRoleable<Guid>.EntityId { get; set; }
    }

    [SecureWith(typeof(ExplicitLink))]
    public class ExplicitProps : IEntity<Guid> { public Guid Id { get; set; } }

    [Theory]
    [InlineData(typeof(HopWithoutVia), "Via is required")]
    [InlineData(typeof(TerminalWithVia), "Via is forbidden")]
    [InlineData(typeof(MissingViaProperty), "does not exist")]
    [InlineData(typeof(ViaTypeMismatch), "keyed by")]
    [InlineData(typeof(KeyMismatch), "keyed by")]
    [InlineData(typeof(CycleA), "cyclic")]
    [InlineData(typeof(NonTerminating), "does not terminate")]
    [InlineData(typeof(SelfTerminal), "its own role table")]
    [InlineData(typeof(ClaimsDerivedCreator), "claims-derived")]
    [InlineData(typeof(ExplicitProps), "must expose a public")]
    public void Misdeclaration_ThrowsWithDefectNamed(Type entity, string messagePart)
    {
        var ex = Assert.Throws<InvalidOperationException>(() => SecureWithChains.TryResolve(entity));
        Assert.Contains(messagePart, ex.Message);
    }
}
