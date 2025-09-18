global using System;
global using System.Text;
global using System.Text.Json;
global using System.Text.Json.Serialization;
global using System.Reflection;
global using System.Diagnostics;
global using System.Collections.Concurrent;

global using Sencilla.Core;
global using Sencilla.Core.Impl;

global using Microsoft.AspNetCore.Http;
global using Microsoft.AspNetCore.Builder;
global using Microsoft.Extensions.Configuration;
global using Microsoft.Extensions.DependencyInjection;
global using Microsoft.Extensions.DependencyInjection.Extensions;

global using System.Linq.Expressions;
global using Microsoft.Extensions.Hosting;

[assembly: AutoDiscovery]

namespace Microsoft.Extensions.DependencyInjection;

public static class Bootstrap
{
    /// <summary>
    /// Add components to Sencilla.
    /// This is empty method just to reference 
    /// component's assambly so it will be loaded 
    /// and Sencilla will register everything 
    /// </summary>
    public static IServiceCollection AddSencillaComponents(this IServiceCollection builder, params Type[] components)
    {
        return builder;
    }

    /// <summary>
    /// Add Sencilla to the application.
    /// </summary>ı
    /// <param name="builder"></param>
    public static IHostApplicationBuilder AddSencilla(this IHostApplicationBuilder builder)
    {
        builder.Services.AddSencilla(builder.Configuration);
        return builder;
    }

    /// <summary>
    /// Register sencilla framework builder, 
    /// ISencillaBuilder must be resolved in configure method to configure sencilla
    /// </summary>
    /// <param name="services"> Service collection </param>
    public static IServiceCollection AddSencilla(this IServiceCollection builder, IConfiguration configuration)
    {
        // create service collection container 
        var container = new ServiceCollectionRegistrator(builder);

        // 0. load all types from app
        var assemblies = AppDomain.CurrentDomain
                             .GetAssemblies()
                             .Where(a => a.GetCustomAttributes(typeof(AutoDiscoveryAttribute), false).Any());

        var types = assemblies.SelectMany(x => x.GetTypes());

        // 1. first find all type registrators in app 
        var registrators = new List<ITypeRegistrator>();
        var typeRegistrator = typeof(ITypeRegistrator);
        foreach (var type in types)
        {
            if (typeRegistrator.IsAssignableFrom(type) && type.IsClass && !type.IsAbstract)
            {
                // create this registrator 
                var registrator = Activator.CreateInstance(type) as ITypeRegistrator;
                if (registrator != null)
                {
                    registrators.Add(registrator);
                    container.RegisterInstance(type, registrator);
                }
            }
        }

        // 2. register all types by registartors
        foreach (var type in types)
        {
            foreach (var r in registrators)
                r.Register(container, type);
        }

        // 3. Add sencilla app as singleton 
        builder.TryAddSingleton<SencillaApp>();
        builder.TryAddSingleton<ISencillaApp>(sp => sp.GetRequiredService<SencillaApp>());

        return builder;
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="app"></param>
    public static void UseSencilla(this IApplicationBuilder app)
    {
        //app.UseServiceLocator();
        // add other components here...
    }

}
