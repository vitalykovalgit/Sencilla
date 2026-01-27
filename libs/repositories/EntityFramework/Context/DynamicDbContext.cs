namespace Sencilla.Repository.EntityFramework;

[DisableInjection]
public class DynamicDbContext([NotNull] DbContextOptions options) : DbContext(options)
{
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        //foreach (var e in registrator.Entities)
        //    BuildModelForEntity(modelBuilder, e);

        foreach (var e in RepositoryEntityFrameworkBootstrap.Entities)
            BuildModelForEntity(modelBuilder, e);

        foreach (var assembly in RepositoryEntityFrameworkBootstrap.Assemblies)
            modelBuilder.ApplyConfigurationsFromAssembly(assembly);

        base.OnModelCreating(modelBuilder);
    }

    public void BuildModelForEntity(ModelBuilder modelBuilder, Type entityType)
    {
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
