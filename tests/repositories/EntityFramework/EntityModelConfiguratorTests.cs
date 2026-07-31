using System.Linq.Dynamic.Core;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore.Infrastructure;
using System.Reflection;
using Microsoft.Extensions.DependencyInjection;

namespace Sencilla.Repository.EntityFramework.Tests;

/// <summary>
/// Two things, because they are the same mechanism seen from both ends.
///
/// <para>First, <see cref="IEntityModelConfigurator"/> — the seam a component uses to teach
/// <see cref="DynamicDbContext"/> about ITS marker interfaces without the context referencing the component. A
/// configurator must be able to both constrain and remove a mapped property.</para>
///
/// <para>Second, the property the tags component uses it for: a <c>List&lt;string&gt;</c> has to survive
/// <see cref="DynamicDbContext.BuildModelForEntity"/>, whose reflection mapper classifies EVERY non-string class
/// property as a navigation and therefore never calls <c>c.Property(...)</c> for it — EF's own conventions must
/// pick it up as a primitive collection, and <c>Contains</c> must translate to SQL.</para>
///
/// <para>Run against Sqlite deliberately: a real relational provider proves mapping + translation + round-trip;
/// InMemory would prove none of them.</para>
/// </summary>
public class EntityModelConfiguratorTests : IDisposable
{
    private readonly ProbeContext _context = ProbeContext.Create(new MaxLengthConfigurator());

    [Fact]
    public void ListOfString_IsMappedAsPrimitiveCollection_NotNavigation()
    {
        var entityType = _context.Model.FindEntityType(typeof(ProbeEntity));

        Assert.NotNull(entityType);
        Assert.Null(entityType!.FindNavigation(nameof(ProbeEntity.Tags)));

        var tags = entityType.FindProperty(nameof(ProbeEntity.Tags));
        Assert.NotNull(tags);
        Assert.NotNull(tags!.GetElementType());   // primitive collection, not an opaque blob
    }

    [Fact]
    public void Configurator_CanConstrainAProperty()
    {
        // Without a max length EF sends nvarchar(-1) parameters at a bounded column — the reason the tags
        // component configures its inline column at all.
        var tags = _context.Model.FindEntityType(typeof(ProbeEntity))!.FindProperty(nameof(ProbeEntity.Tags));

        Assert.Equal(4000, tags!.GetMaxLength());
    }

    [Fact]
    public void Configurator_CanRemoveAProperty()
    {
        using var context = ProbeContext.Create(new IgnoreConfigurator());

        Assert.Null(context.Model.FindEntityType(typeof(ProbeEntity))!.FindProperty(nameof(ProbeEntity.Tags)));
    }

    [Fact]
    public void NoConfigurators_LeavesTheConventionsAlone()
    {
        // The context is constructed by hand (as in tests) or by a host with no component supplying one.
        using var context = ProbeContext.Create();
        var tags = context.Model.FindEntityType(typeof(ProbeEntity))!.FindProperty(nameof(ProbeEntity.Tags));

        Assert.NotNull(tags);
        Assert.Null(tags!.GetMaxLength());
    }

    [Fact]
    public void Configurators_ReachTheContextThroughDependencyInjection()
    {
        // The whole seam hangs on EF resolving the context's optional constructor parameter from the
        // application service provider. If it ever stopped doing that, every component-supplied mapping would
        // silently vanish — a wrong column, not an exception. Read back through reflection so proving the
        // wiring costs no production API.
        var services = new ServiceCollection();
        services.AddSingleton<IEntityModelConfigurator, MaxLengthConfigurator>();
        services.AddDbContext<DynamicDbContext>(o => o.UseSqlite("DataSource=di_probe;Mode=Memory;Cache=Shared"));

        using var provider = services.BuildServiceProvider();
        using var scope = provider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<DynamicDbContext>();

        var field = typeof(DynamicDbContext).GetField("_configurators", BindingFlags.Instance | BindingFlags.NonPublic);
        var configurators = (IReadOnlyList<IEntityModelConfigurator>)field!.GetValue(context)!;

        Assert.Single(configurators);
        Assert.IsType<MaxLengthConfigurator>(configurators[0]);
    }

    [Fact]
    public async Task Contains_TranslatesToSql_AndFiltersRows()
    {
        await SeedAsync();

        var matched = await _context.Set<ProbeEntity>()
            .Where(t => t.Tags!.Contains("summer"))
            .Select(t => t.Name)
            .ToListAsync();

        Assert.Equal(["tagged-both", "tagged-summer"], matched.Order());
    }

    [Fact]
    public async Task DynamicLinq_ContainsAny_TranslatesToSql()
    {
        await SeedAsync();

        // The exact shape InlineTagRepository.FilterAny composes for ?tag=a&tag=b (ANY).
        var matched = await _context.Set<ProbeEntity>()
            .Where("Tags != null && (Tags.Contains(@0) || Tags.Contains(@1))", "winter", "summer")
            .Select(t => t.Name)
            .ToDynamicListAsync<string>();

        Assert.Equal(["tagged-both", "tagged-summer", "tagged-winter"], matched.Order());
    }

    [Fact]
    public async Task NullColumn_RoundTripsAsNull_NotEmptyList()
    {
        await SeedAsync();

        var untagged = await _context.Set<ProbeEntity>().SingleAsync(t => t.Name == "untagged");

        Assert.Null(untagged.Tags);
    }

    private async Task SeedAsync()
    {
        _context.AddRange(
            new ProbeEntity { Name = "tagged-summer", Tags = ["summer"] },
            new ProbeEntity { Name = "tagged-winter", Tags = ["winter"] },
            new ProbeEntity { Name = "tagged-both", Tags = ["summer", "winter"] },
            new ProbeEntity { Name = "untagged", Tags = null });

        await _context.SaveChangesAsync();
        _context.ChangeTracker.Clear();
    }

    public void Dispose()
    {
        _context.Dispose();
        GC.SuppressFinalize(this);
    }

    private class ProbeEntity : IEntity<int>
    {
        public int Id { get; set; }
        public string Name { get; set; } = "";
        public List<string>? Tags { get; set; }
    }

    /// <summary>Stands in for the tags component's configurator: bound the collection column.</summary>
    private sealed class MaxLengthConfigurator : IEntityModelConfigurator
    {
        public void Configure(EntityTypeBuilder builder, Type entityType)
            => builder.Property(nameof(ProbeEntity.Tags)).HasMaxLength(4000);
    }

    /// <summary>The other half: a component whose storage lives elsewhere removes the phantom column.</summary>
    private sealed class IgnoreConfigurator : IEntityModelConfigurator
    {
        public void Configure(EntityTypeBuilder builder, Type entityType)
            => builder.Ignore(nameof(ProbeEntity.Tags));
    }

    /// <summary>
    /// Builds its model with <see cref="DynamicDbContext.BuildModelForEntity"/> — the code under test — instead
    /// of hand-written configuration, passing the configurators the way DI passes them in a real host.
    /// </summary>
    private sealed class ProbeContext(DbContextOptions<ProbeContext> options, IEnumerable<IEntityModelConfigurator> configurators) : DbContext(options)
    {
        /// <summary>
        /// EF caches the built model per context TYPE, so without a per-instance cache key every probe here
        /// would silently reuse the first model built and the configurator under test would never run.
        /// </summary>
        private readonly Guid _modelKey = Guid.NewGuid();

        public static ProbeContext Create(params IEntityModelConfigurator[] configurators)
        {
            var options = new DbContextOptionsBuilder<ProbeContext>()
                .UseSqlite($"DataSource=configurator_{Guid.NewGuid():N};Mode=Memory;Cache=Shared")
                .ReplaceService<IModelCacheKeyFactory, PerInstanceModelCacheKeyFactory>()
                .Options;

            var context = new ProbeContext(options, configurators);
            context.Database.OpenConnection();
            context.Database.EnsureCreated();
            return context;
        }

        private sealed class PerInstanceModelCacheKeyFactory : IModelCacheKeyFactory
        {
            public object Create(DbContext context, bool designTime)
                => (context.GetType(), ((ProbeContext)context)._modelKey, designTime);
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            using var probe = new DynamicDbContext(new DbContextOptionsBuilder<DynamicDbContext>().Options, configurators);
            probe.BuildModelForEntity(modelBuilder, typeof(ProbeEntity));
        }

        public override void Dispose()
        {
            Database.CloseConnection();
            base.Dispose();
        }
    }
}
