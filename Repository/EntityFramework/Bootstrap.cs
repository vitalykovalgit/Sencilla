global using System;
global using System.Text;
global using System.Linq;
global using System.Linq.Expressions;
global using System.Linq.Dynamic.Core;
global using System.Reflection;
global using System.Diagnostics.CodeAnalysis;
global using System.ComponentModel.DataAnnotations.Schema;

global using Sencilla.Core;
global using Sencilla.Repository.EntityFramework;
global using Microsoft.EntityFrameworkCore;

[assembly: AutoDiscovery]

namespace Microsoft.Extensions.DependencyInjection
{
    public static class Bootstrap
    {
        public static IServiceCollection AddSencillaRepositoryForEF(this IServiceCollection builder, Action<DbContextOptionsBuilder> action)
        {
            //builder.AddTransient<RepositoryDependency>();
            builder.AddDbContext<DynamicDbContext>(action);
            return builder;
        }
    }
}
