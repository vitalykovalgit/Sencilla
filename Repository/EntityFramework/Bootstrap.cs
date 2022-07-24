
using Microsoft.EntityFrameworkCore;
using Sencilla.Repository.EntityFramework;

namespace Microsoft.Extensions.DependencyInjection
{
    public static class Bootstrap
    {
        public static IServiceCollection AddSencillaEntityFrameworkRepos(this IServiceCollection builder, Action<DbContextOptionsBuilder> action)
        {
            builder.AddDbContext<DynamicDbContext>(action);
            return builder;
        }
    }
}
