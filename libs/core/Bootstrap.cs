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
    public static IServiceCollection AddSencilla(this IServiceCollection services, IConfiguration configuration)
    {

        // 0. Force-load the app's full assembly reference graph first. The CLR loads
        // assemblies lazily, and the [AutoDiscovery] scan below can only see assemblies
        // ALREADY in the AppDomain — without this walk, whether a component's types (its
        // repositories, handlers, security constraints, ...) get registered depends on
        // JIT/debugger load order and differs run to run.
        var visited = new HashSet<string>(AppDomain.CurrentDomain.GetAssemblies().Select(a => a.GetName().FullName));
        var pending = new Queue<Assembly>(AppDomain.CurrentDomain.GetAssemblies());
        while (pending.Count > 0)
        {
            foreach (var reference in pending.Dequeue().GetReferencedAssemblies())
            {
                if (!visited.Add(reference.FullName))
                    continue;

                try { pending.Enqueue(Assembly.Load(reference)); }
                catch { /* native / reference-only / trimmed assembly — nothing to scan */ }
            }
        }

        // load all types from app
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
                    //services.TryAddSingleton(registrator);
                    services.AddSingleton(type, registrator);
                }
            }
        }

        // 2. register all types by registartors
        foreach (var type in types)
        {
            foreach (var r in registrators)
                r.Register(services, type);
        }

        // 3. Framework primitives that discovered components ctor-inject but bare (non-web)
        // hosts never register — e.g. Security's RoleClosure caches the role graph via
        // IMemoryCache, and Users' CurrentUserProvider reads IHttpContextAccessor (whose
        // HttpContext is simply null outside a request — the correct "no current user"
        // semantics for background hosts). TryAdd semantics inside both: an app's own
        // AddMemoryCache(configure) still applies its options regardless of call order.
        services.AddMemoryCache();
        services.AddHttpContextAccessor();

        // 4. Add sencilla app as singleton
        services.TryAddSingleton<SencillaApp>();
        services.TryAddSingleton<ISencillaApp>(sp => sp.GetRequiredService<SencillaApp>());

        return services;
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
