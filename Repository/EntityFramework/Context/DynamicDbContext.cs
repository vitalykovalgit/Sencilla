

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
                    if (nma == null)
                    {
                        var na = p.GetCustomAttribute<ColumnAttribute>();
                        c.Property(p.Name).HasColumnName(na?.Name ?? p.Name);
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
