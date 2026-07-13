using System.Linq.Expressions;
using Microsoft.Extensions.Caching.Memory;
using Moq;
using Sencilla.Core;
using Sencilla.Component.Users;

namespace Sencilla.Component.Security.Tests;

public class SecurityConstraintWritePathTests
{
    private class TestOrder
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
    }

    private readonly SecurityConstraintHandler<TestOrder> _handler = new();
    private readonly Mock<ISecurityProvider> _provider = new();
    private readonly ISystemVariable _sysVars = Mock.Of<ISystemVariable>(); // null user → Anonymous only
    private readonly IMemoryCache _cache = new MemoryCache(new MemoryCacheOptions());
    private readonly IReadRepository<UserRole, Guid> _userRoles = Mock.Of<IReadRepository<UserRole, Guid>>();
    private readonly SecurityCacheSignal _signal = new();
    private readonly IRoleClosure _closure = IdentityClosure();
    private readonly List<IPermissionRule<TestOrder>> _rules = [];

    private static IRoleClosure IdentityClosure()
    {
        var closure = new Mock<IRoleClosure>();
        closure.Setup(c => c.Expand(It.IsAny<HashSet<int>>())).Returns<HashSet<int>>(ids => ids);
        return closure.Object;
    }

    private void GrantAnonymous(params string?[] constraints) =>
        _provider
            .Setup(p => p.Permissions<TestOrder>(It.IsAny<CancellationToken>(), It.IsAny<Action?>()))
            .ReturnsAsync(constraints
                .Select(c => new Matrix { RoleId = (int)RoleType.Anonymous, Resource = nameof(TestOrder), Action = (int)Action.Update, Constraint = c })
                .ToArray());

    private void GrantNothing() =>
        _provider
            .Setup(p => p.Permissions<TestOrder>(It.IsAny<CancellationToken>(), It.IsAny<Action?>()))
            .ReturnsAsync(Array.Empty<Matrix>());

    private readonly IServiceProvider _services = Mock.Of<IServiceProvider>();

    private Task Handle(EntityUpdatingEvent<TestOrder> e) =>
        _handler.HandleAsync(e, _sysVars, _provider.Object, _cache, _userRoles, _signal, _closure, _rules, _services, CancellationToken.None);

    private Task Handle(EntityUpdatedEvent<TestOrder> e) =>
        _handler.HandleAsync(e, _sysVars, _provider.Object, _cache, _userRoles, _signal, _closure, _rules, _services, CancellationToken.None);

    private Task Handle(EntityCreatingEvent<TestOrder> e) =>
        _handler.HandleAsync(e, _sysVars, _provider.Object, _cache, _userRoles, _signal, _closure, _rules, _services, CancellationToken.None);

    private Task Handle(EntityCreatedEvent<TestOrder> e) =>
        _handler.HandleAsync(e, _sysVars, _provider.Object, _cache, _userRoles, _signal, _closure, _rules, _services, CancellationToken.None);

    private static IQueryable<TestOrder> Rows(params string[] names) =>
        names.Select((n, i) => new TestOrder { Id = i + 1, Name = n }).AsQueryable();

    [Fact]
    public async Task Updating_ComposesConstraintIntoDbQuery_PreImage()
    {
        GrantAnonymous("Name == \"mine\"");
        var e = new EntityUpdatingEvent<TestOrder> { DbEntities = Rows("mine", "theirs") };

        await Handle(e);

        // Narrowed, not thrown — the repository compares counts and decides.
        Assert.Equal(1, e.DbEntities!.Count());
        Assert.Equal("mine", e.DbEntities!.Single().Name);
    }

    [Fact]
    public async Task Updated_ComposesConstraintIntoDbQuery_PostImage()
    {
        GrantAnonymous("Name == \"mine\"");
        var e = new EntityUpdatedEvent<TestOrder> { DbEntities = Rows("given-away") };

        await Handle(e);

        Assert.Equal(0, e.DbEntities!.Count());
    }

    [Fact]
    public async Task Updating_UnconstrainedGrant_LeavesQueryUntouched()
    {
        GrantAnonymous((string?)null);
        var query = Rows("anything");
        var e = new EntityUpdatingEvent<TestOrder> { DbEntities = query };

        await Handle(e);

        Assert.Same(query, e.DbEntities);
    }

    [Fact]
    public async Task Updating_NoApplicablePermissions_Throws()
    {
        GrantNothing();
        var e = new EntityUpdatingEvent<TestOrder> { DbEntities = Rows("mine") };

        await Assert.ThrowsAsync<ForbiddenException>(() => Handle(e));
    }

    [Fact]
    public async Task Updating_WithoutDbQuery_FallsBackToInMemoryValidation()
    {
        // Non-EF publishers supply no database query; the legacy in-memory check
        // still guards them (client objects validated directly).
        GrantAnonymous("Name == \"mine\"");
        var e = new EntityUpdatingEvent<TestOrder> { Entities = Rows("theirs") };

        await Assert.ThrowsAsync<ForbiddenException>(() => Handle(e));
    }

    // ── Row combination: grants accumulate (OR everywhere) ───────────────────

    [Fact]
    public async Task ConstrainedRows_SameRole_CombineWithOr()
    {
        // Regression guard — rows used to AND within a role, so adding a matrix
        // row silently NARROWED access. Grants must accumulate.
        GrantAnonymous("Name == \"a\"", "Name == \"b\"");
        var e = new EntityUpdatingEvent<TestOrder> { DbEntities = Rows("a", "b", "c") };

        await Handle(e);

        Assert.Equal(2, e.DbEntities!.Count());
    }

    [Fact]
    public async Task UnconstrainedRow_AmongConstrained_IsFullGrant()
    {
        GrantAnonymous("Name == \"a\"", null);
        var query = Rows("whatever");
        var e = new EntityUpdatingEvent<TestOrder> { DbEntities = query };

        await Handle(e);

        Assert.Same(query, e.DbEntities);
    }

    // ── Root bypass ──────────────────────────────────────────────────────────

    [Fact]
    public async Task RootRole_BypassesChecks_EvenWithEmptyMatrix()
    {
        GrantNothing();
        var root = new User { Email = "root@example.com" };
        root.AddRole((int)RoleType.Root);
        var sysVars = Mock.Of<ISystemVariable>(s => s.Get<User>("user") == root);

        var query = Rows("anything");
        var e = new EntityUpdatingEvent<TestOrder> { DbEntities = query };

        await _handler.HandleAsync(e, sysVars, _provider.Object, _cache, _userRoles, _signal, _closure, _rules, _services, CancellationToken.None);

        Assert.Same(query, e.DbEntities);
    }

    // ── Create: applicability gate on -ing, enforcement on the -ed post-image ─

    [Fact]
    public async Task Creating_NoApplicablePermissions_Throws()
    {
        GrantNothing();
        var e = new EntityCreatingEvent<TestOrder> { Entities = Rows("mine") };

        await Assert.ThrowsAsync<ForbiddenException>(() => Handle(e));
    }

    [Fact]
    public async Task Creating_ConstrainedGrant_PassesWithoutFiltering()
    {
        // The gate only checks that SOME grant source exists — constraint evaluation
        // is deferred to the post-image, where DB state (role rows, parents) exists.
        GrantAnonymous("Name == \"mine\"");
        var query = Rows("theirs");
        var e = new EntityCreatingEvent<TestOrder> { Entities = query };

        await Handle(e);

        Assert.Same(query, e.Entities);
    }

    [Fact]
    public async Task Created_ComposesConstraintIntoDbQuery_PostImage()
    {
        GrantAnonymous("Name == \"mine\"");
        var e = new EntityCreatedEvent<TestOrder> { DbEntities = Rows("mine", "theirs") };

        await Handle(e);

        // Narrowed, not thrown — the repository compares counts and rolls back.
        Assert.Equal(1, e.DbEntities!.Count());
    }

    [Fact]
    public async Task Created_UnconstrainedGrant_LeavesQueryUntouched()
    {
        GrantAnonymous((string?)null);
        var query = Rows("anything");
        var e = new EntityCreatedEvent<TestOrder> { DbEntities = query };

        await Handle(e);

        Assert.Same(query, e.DbEntities);
    }

    [Fact]
    public async Task Created_WithoutDbQuery_FallsBackToInMemoryValidation()
    {
        // Non-EF publishers supply no post-image; the in-memory count check guards them.
        GrantAnonymous("Name == \"mine\"");
        var e = new EntityCreatedEvent<TestOrder> { Entities = Rows("theirs") };

        await Assert.ThrowsAsync<ForbiddenException>(() => Handle(e));
    }

    // ── Code-declared rules (specification pattern) ──────────────────────────

    private sealed class NamedOrderRule(string name) : IPermissionRule<TestOrder>
    {
        public int RoleId => (int)RoleType.Anonymous;
        public Action Action => Action.Update;
        public Expression<Func<TestOrder, bool>>? Constraint(ISystemVariable sysVars) =>
            name == "*" ? null : o => o.Name == name;
    }

    [Fact]
    public async Task CodeRule_GrantsWithoutAnyMatrixRow()
    {
        GrantNothing();
        _rules.Add(new NamedOrderRule("mine"));
        var e = new EntityUpdatingEvent<TestOrder> { DbEntities = Rows("mine", "theirs") };

        await Handle(e);

        Assert.Equal("mine", e.DbEntities!.Single().Name);
    }

    [Fact]
    public async Task CodeRule_UnionsWithMatrixRows()
    {
        GrantAnonymous("Name == \"a\"");
        _rules.Add(new NamedOrderRule("b"));
        var e = new EntityUpdatingEvent<TestOrder> { DbEntities = Rows("a", "b", "c") };

        await Handle(e);

        Assert.Equal(2, e.DbEntities!.Count());
    }

    [Fact]
    public async Task CodeRule_Unconditional_IsFullGrant()
    {
        GrantNothing();
        _rules.Add(new NamedOrderRule("*"));
        var query = Rows("anything");
        var e = new EntityUpdatingEvent<TestOrder> { DbEntities = query };

        await Handle(e);

        Assert.Same(query, e.DbEntities);
    }

    // ── Inheritance: closure expansion feeds matrix matching ─────────────────

    [Fact]
    public async Task InheritedRole_GrantsParentPermissions()
    {
        // Matrix row belongs to role 100 (a custom parent); the user only has
        // Anonymous, but the closure expands Anonymous -> 100.
        _provider
            .Setup(p => p.Permissions<TestOrder>(It.IsAny<CancellationToken>(), It.IsAny<Action?>()))
            .ReturnsAsync(new[] { new Matrix { RoleId = 100, Resource = nameof(TestOrder), Action = (int)Action.Update, Constraint = null } });

        var closure = new Mock<IRoleClosure>();
        closure.Setup(c => c.Expand(It.IsAny<HashSet<int>>()))
               .Returns<HashSet<int>>(ids => [.. ids, 100]);

        var query = Rows("anything");
        var e = new EntityUpdatingEvent<TestOrder> { DbEntities = query };

        await _handler.HandleAsync(e, _sysVars, _provider.Object, _cache, _userRoles, _signal, closure.Object, _rules, _services, CancellationToken.None);

        Assert.Same(query, e.DbEntities);
    }
}
