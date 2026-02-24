namespace Sencilla.Repository.EntityFramework.Tests.Infrastructure;

// ── Read ──────────────────────────────────────────────────────────────────────
/// <summary>Concrete ReadRepository for TestProduct used in read-only tests.</summary>
public class TestReadRepository(RepositoryDependency dep, TestDbContext ctx)
    : ReadRepository<TestProduct, TestDbContext>(dep, ctx);

// ── Create ────────────────────────────────────────────────────────────────────
/// <summary>Concrete CreateRepository for TestProduct (includes all read operations).</summary>
public class TestCreateRepository(RepositoryDependency dep, TestDbContext ctx)
    : CreateRepository<TestProduct, TestDbContext>(dep, ctx);

// ── Update ────────────────────────────────────────────────────────────────────
/// <summary>Concrete UpdateRepository for TestProduct (includes all read operations).</summary>
public class TestUpdateRepository(RepositoryDependency dep, TestDbContext ctx)
    : UpdateRepository<TestProduct, TestDbContext>(dep, ctx);

// ── Delete ────────────────────────────────────────────────────────────────────
/// <summary>Concrete DeleteRepository for TestProduct (hard delete).</summary>
public class TestDeleteRepository(RepositoryDependency dep, TestDbContext ctx)
    : DeleteRepository<TestProduct, TestDbContext>(dep, ctx);

// ── Remove ────────────────────────────────────────────────────────────────────
/// <summary>Concrete RemoveRepository for TestProduct (soft delete, extends Update).</summary>
public class TestRemoveRepository(RepositoryDependency dep, TestDbContext ctx)
    : RemoveRepository<TestProduct, TestDbContext>(dep, ctx);
