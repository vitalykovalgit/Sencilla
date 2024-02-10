using System.Diagnostics.CodeAnalysis;

namespace Sencilla.Repository.EntityFramework
{
    [DisableInjection]
    public class DynamicDbContext : DbContext
    {
        RepositoryRegistrator Registrator;

        public DynamicDbContext(
            [NotNull] DbContextOptions<DynamicDbContext> options,
            RepositoryRegistrator registrator) : base(options)
        {
            Registrator = registrator;
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            foreach (var e in Registrator.ContextEntitiesPairs[nameof(DynamicDbContext)])
            {
                /*
                if (e.Name == "User")
                {
                    modelBuilder.Entity(e).HasOne("UserInfo").WithOne().HasForeignKey("UserInfo", "Id");
                    modelBuilder.Entity(e).ToTable("User").Property("Email").HasColumnName("Email");
                }
                else
                {
                    modelBuilder.Entity(e);
                }*/
                modelBuilder.Entity(e);
            }

            base.OnModelCreating(modelBuilder);
        }
    }

    [DisableInjection]
    public class UserInfoDynamicDbContext : DbContext
    {
        RepositoryRegistrator Registrator;

        public UserInfoDynamicDbContext(
            [NotNull] DbContextOptions<UserInfoDynamicDbContext> options,
            RepositoryRegistrator registrator) : base(options)
        {
            Registrator = registrator;
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            foreach (var e in Registrator.ContextEntitiesPairs[nameof(UserInfoDynamicDbContext)])
            {
                //modelBuilder.Entity(e).ToTable("User").Property("Email").HasColumnName("Email");
                modelBuilder.Entity(e);
            }

            base.OnModelCreating(modelBuilder);
        }
    }
}
