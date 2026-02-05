global using System;
global using System.ComponentModel.DataAnnotations.Schema;
global using System.Diagnostics.CodeAnalysis;
global using System.Linq;
global using System.Linq.Dynamic.Core;
global using System.Linq.Expressions;
global using System.Reflection;
global using System.Text;
global using System.Text.Json;
global using System.Diagnostics;

global using Microsoft.EntityFrameworkCore;
global using Microsoft.EntityFrameworkCore.Query;
global using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
global using Microsoft.Extensions.DependencyInjection;
global using Microsoft.Extensions.DependencyInjection.Extensions;
global using Microsoft.Extensions.Hosting;

global using Sencilla.Core;
global using Sencilla.Repository.EntityFramework;

[assembly: AutoDiscovery]

namespace Microsoft.Extensions.DependencyInjection;

public static class RepositoryEntityFrameworkBootstrap
{
    public static List<Type> Entities { get; } = [];
    public static List<Assembly> Assemblies { get; } = [];

    public static IHostApplicationBuilder AddSencillaRepositoryForEF(this IHostApplicationBuilder builder, Action<DbContextOptionsBuilder> configure)
    {
        return builder.AddSencillaRepositoryForEF(configure);
    }

    public static IServiceCollection AddSencillaRepositoryForEF(this IServiceCollection builder, Action<DbContextOptionsBuilder> configure)
    {
        // Try to add repository for calling assembly
        // Get calling asseambly
        var assembly = new StackFrame(1).GetMethod()?.DeclaringType?.Assembly;
        if (assembly != null)
        {
            if (!Assemblies.Contains(assembly))
                Assemblies.Add(assembly);

            foreach (var type in assembly.GetTypes())
            {
                builder.RegisterEFContexts(type, configure);

                builder.RegisterEFRepositoriesForType(type, out bool isAdded);
                if (isAdded && !Entities.Contains(type))
                    Entities.Add(type);
            }
        }

        builder.TryAddScoped<RepositoryDependency>();
        builder.AddDbContext<DynamicDbContext>(configure);

        builder.AddEntityFrameworkCoreExtensions();
        return builder;
    }
}
