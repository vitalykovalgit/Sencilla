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

    public static IServiceCollection AddSencillaRepositories(this IServiceCollection builder)
    {
        var assembly = new StackFrame(1).GetMethod()?.DeclaringType?.Assembly;

        if (assembly != null && !Assemblies.Contains(assembly))
            Assemblies.Add(assembly);

        return builder;
    }


    public static IServiceCollection AddSencillaRepositoryForEF(this IServiceCollection builder, Action<DbContextOptionsBuilder> configure)
    {
        // Try to add repository for calling assembly
        // Get calling assembly
        var assembly = new StackFrame(1).GetMethod()?.DeclaringType?.Assembly;
        if (assembly != null && !Assemblies.Contains(assembly))
            Assemblies.Add(assembly);

        builder.AddSencillaEFRepositoryForAssemblies(configure);

        builder.TryAddScoped<RepositoryDependency>();
        builder.AddDbContext<DynamicDbContext>(configure);

        builder.AddEntityFrameworkCoreExtensions();
        return builder;
    }

    public static IServiceCollection AddSencillaEFRepositoryForAssemblies(this IServiceCollection builder, Action<DbContextOptionsBuilder> configure)
    {
        foreach (var assembly in Assemblies)
            builder.AddSencillaEFRepositoryForAssembly(assembly, configure);
        return builder;
    }


    public static IServiceCollection AddSencillaEFRepositoryForAssembly(this IServiceCollection builder, Assembly assembly, Action<DbContextOptionsBuilder> configure)
    {
        foreach (var type in assembly.GetTypes())
        {
            // builder.RegisterEFFilters(type); disable for now as we need to add only one registartion be combination interface - implemenattion
            builder.RegisterEFContexts(type, configure);
            builder.RegisterEFRepositoriesForType(type, out bool isAdded);
            if (isAdded && !Entities.Contains(type))
                Entities.Add(type);
        }
        return builder;
    }

    private static Assembly? GetCallingAssembly()
    {
        var currentAssembly = Assembly.GetExecutingAssembly();

        for (int frameIndex = 1; frameIndex < 10; frameIndex++)
        {
            var frame = new StackFrame(frameIndex);
            var method = frame.GetMethod();
            var assembly = method?.DeclaringType?.Assembly;

            if (assembly != null && assembly != currentAssembly)
                return assembly;
        }

        return null;
    }

}
