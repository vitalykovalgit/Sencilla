global using System;
global using System.Text;
global using System.Linq;
global using System.Linq.Dynamic.Core;

global using Sencilla.Core;
global using Sencilla.Repository.EntityFramework;

global using Microsoft.EntityFrameworkCore;

namespace Microsoft.Extensions.DependencyInjection
{
    public static class Bootstrap
    {
        public static IServiceCollection AddSencillaRepositoryForEF(this IServiceCollection builder, Action<DbContextOptionsBuilder> action)
        {
            builder.AddDbContext<DynamicDbContext>(action);
            return builder;
        }
    }
}
