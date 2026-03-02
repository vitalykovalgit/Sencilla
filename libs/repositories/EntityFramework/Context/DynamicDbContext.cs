namespace Sencilla.Repository.EntityFramework;

[DisableInjection]
public class DynamicDbContext([NotNull] DbContextOptions options) : DbContext(options)
{
    private static IModel? _compiledModel;
    private static readonly object _modelLock = new();

    /// <summary>
    /// Pre-compiles and caches the EF Core model at startup,
    /// so subsequent DbContext instances skip OnModelCreating entirely.
    /// Call this from Program.cs after building the host:
    /// <code>app.WarmUpEFModel();</code>
    /// </summary>
    internal static void CompileModel(IServiceProvider serviceProvider)
    {
        if (_compiledModel != null) return;
        lock (_modelLock)
        {
            if (_compiledModel != null) return;
            using var scope = serviceProvider.CreateScope();
            var ctx = scope.ServiceProvider.GetRequiredService<DynamicDbContext>();
            _compiledModel = ctx.Model;
        }
    }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        if (_compiledModel != null)
        {
            optionsBuilder.UseModel(_compiledModel);
        }
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        foreach (var e in RepositoryEntityFrameworkBootstrap.Entities)
            BuildModelForEntity(modelBuilder, e);

        foreach (var assembly in RepositoryEntityFrameworkBootstrap.Assemblies)
            modelBuilder.ApplyConfigurationsFromAssembly(assembly);

        base.OnModelCreating(modelBuilder);
    }

    public void BuildModelForEntity(ModelBuilder modelBuilder, Type entityType)
    {
        // ignore if has DbContextAttribute 
        var customDbContext = entityType.GetCustomAttribute(typeof(DbContextAttribute<>));
        if (customDbContext != null)
        {
            return;
        }

        modelBuilder.Entity(entityType, (c) =>
        {
            var ta = entityType.GetCustomAttribute<TableAttribute>();
            c.ToTable(ta?.Name ?? entityType.Name, ta?.Schema ?? "dbo");

            // map properties
            var properties = entityType.GetProperties();
            foreach (var p in properties)
            {
                // map all properties which are not marked with [NotMapped]
                var nma = p.GetCustomAttribute<NotMappedAttribute>();
                var na = p.GetCustomAttribute<ColumnAttribute>();
                var fka = p.GetCustomAttribute<ForeignKeyAttribute>();
                var ipa = p.GetCustomAttribute<InversePropertyAttribute>();

                var isMapped = nma is null;
                // TODO: extend it later for case property has a custom value converter
                var isNavigationProperty =
                    na is null && (
                        fka is not null ||
                        ipa is not null ||
                        p.PropertyType.IsClass && p.PropertyType != typeof(string) ||
                        p.PropertyType.IsInterface
                    );

                if (isMapped && !isNavigationProperty)
                    c.Property(p.Name).HasColumnName(na?.Name ?? p.Name);

                var joa = p.GetCustomAttribute<JsonObjectAttribute>();
                if (joa is not null)
                    c.Property(p.Name).HasConversion(JsonObjectConverter<object>.CreateConverter(p.PropertyType));
            }

            // check if entity map to the same table 
            var pe = entityType.GetCustomAttribute<MainEntityAttribute>();
            if (pe != null)
                c.HasOne(pe.Type).WithOne().HasForeignKey(entityType);
        });

    }
}
