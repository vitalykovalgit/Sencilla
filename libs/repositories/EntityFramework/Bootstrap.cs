global using System;
global using System.Text;
global using System.Text.Json;
global using System.Linq;
global using System.Linq.Expressions;
global using System.Linq.Dynamic.Core;
global using System.Reflection;
global using System.Diagnostics.CodeAnalysis;
global using System.ComponentModel.DataAnnotations.Schema;

global using Microsoft.Extensions.Hosting;
global using Microsoft.Extensions.DependencyInjection;
global using Microsoft.Extensions.DependencyInjection.Extensions;

global using Microsoft.EntityFrameworkCore;
global using Microsoft.EntityFrameworkCore.Query;
global using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

global using Sencilla.Core;
global using Sencilla.Repository.EntityFramework;

[assembly: AutoDiscovery]

namespace Microsoft.Extensions.DependencyInjection;

public static class Bootstrap
{
    public static IHostApplicationBuilder AddSencillaRepositoryForEF(this IHostApplicationBuilder builder, Action<DbContextOptionsBuilder> configure)
    {
        return builder.AddSencillaRepositoryForEF(configure);
    }

    public static IServiceCollection AddSencillaRepositoryForEF(this IServiceCollection builder, Action<DbContextOptionsBuilder> configure)
    {
        builder.TryAddScoped<RepositoryDependency>();
        builder.AddDbContext<DynamicDbContext>(configure);
        builder.AddEntityFrameworkCoreExtensions();
        return builder;
    }
}
