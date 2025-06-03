

namespace Sencilla.Repository.EntityFramework;

[DisableInjection]
public class DynamicDbContext : DbContext
{
    RepositoryRegistrator Registrator;

    public DynamicDbContext(
        [NotNull] DbContextOptions options,
        RepositoryRegistrator registrator) : base(options)
    {
        Registrator = registrator;
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        foreach (var e in Registrator.Entities)
        {
            modelBuilder.Entity(e, (c) =>
            {
                var ta = e.GetCustomAttribute<TableAttribute>();
                c.ToTable(ta?.Name ?? e.Name, ta?.Schema ?? "dbo");

                // map properties
                var properties = e.GetProperties();
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
                var pe = e.GetCustomAttribute<MainEntityAttribute>();
                if (pe != null)
                    c.HasOne(pe.Type).WithOne().HasForeignKey(e);
            });
        }

        base.OnModelCreating(modelBuilder);
    }
}
