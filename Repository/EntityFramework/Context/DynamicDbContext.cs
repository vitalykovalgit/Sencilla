using Microsoft.EntityFrameworkCore;
using System.Diagnostics.CodeAnalysis;

namespace Sencilla.Repository.EntityFramework
{
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
                modelBuilder.Entity(e);
            }

            //modelBuilder.Entity("someclass");
            //modelBuilder.Entity<Test>().ToTable(typeof(Test).Name);
            base.OnModelCreating(modelBuilder);
        }
    }
}
