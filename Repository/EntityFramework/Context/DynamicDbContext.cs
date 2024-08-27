

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

                    if (nma is null)
                    {
                        var na = p.GetCustomAttribute<ColumnAttribute>();
                        c.Property(p.Name).HasColumnName(na?.Name ?? p.Name);
                    }

                    var joa = p.GetCustomAttribute<JsonObjectAttribute>();
                    if (joa is not null)
                        c.Property(p.Name).HasConversion(JsonObjectConverter<object>.CreateConverter(p.PropertyType));

                    var fka = p.GetCustomAttribute<ForeignKeyAttribute>();
                    var ipa = p.GetCustomAttribute<InversePropertyAttribute>();
                    if (fka is not null && ipa is not null)
                    {
                        string hasOne = p.Name;
                        string withMany = ipa.Property;
                        string hasForeignKey = fka.Name;
                        c.HasOne(hasOne).WithMany(withMany).HasForeignKey(hasForeignKey);
                    }
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
