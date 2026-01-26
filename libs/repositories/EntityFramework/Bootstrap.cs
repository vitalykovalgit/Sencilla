global using Microsoft.EntityFrameworkCore;
global using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
global using Microsoft.Extensions.DependencyInjection;
global using Microsoft.Extensions.DependencyInjection.Extensions;
global using Microsoft.Extensions.Hosting;
global using Sencilla.Core;
global using Sencilla.Repository.EntityFramework;
global using System;
global using System.ComponentModel.DataAnnotations.Schema;
global using System.Diagnostics.CodeAnalysis;
global using System.Linq;
global using System.Linq.Dynamic.Core;
global using System.Linq.Expressions;
global using System.Reflection;
global using System.Text;
global using System.Text.Json;
using Sencilla.Repository.EntityFramework.Extension;

[assembly: AutoDiscovery]

namespace Microsoft.Extensions.DependencyInjection;

public static class Bootstrap
{
    public static IHostApplicationBuilder AddSencillaRepositoryForEF(this IHostApplicationBuilder builder, Action<DbContextOptionsBuilder> configureDb, Action<DatabaseOptions> configureDatabaseOptions)
    {
        builder.AddSencillaRepositoryForEF( configureDb, configureDatabaseOptions);
        return builder;
    }

    public static IServiceCollection AddSencillaRepositoryForEF(this IServiceCollection builder, Action<DbContextOptionsBuilder> configure, Action<DatabaseOptions> configureDb)
    {
        builder.Configure(configureDb);
        builder.TryAddScoped<RepositoryDependency>();
        builder.AddDbContext<DynamicDbContext>(configure);
        builder.AddEntityFrameworkCoreExtensions();
        return builder;
    }
}
