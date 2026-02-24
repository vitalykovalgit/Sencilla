using Microsoft.EntityFrameworkCore;

namespace Sencilla.Repository.EntityFramework.Tests.Infrastructure;

/// <summary>
/// Minimal in-memory DbContext for EF Core repository tests.
/// </summary>
public class TestDbContext : DbContext
{
    public TestDbContext(DbContextOptions<TestDbContext> options) : base(options) { }

    public DbSet<TestProduct> Products { get; set; } = null!;
}
