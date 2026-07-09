global using System.Reflection;
global using System.Text.Json;
global using System.ComponentModel.DataAnnotations.Schema;

global using Microsoft.AspNetCore.Http;
global using Microsoft.AspNetCore.Builder;
global using Microsoft.Extensions.DependencyInjection;

global using Sencilla.Core;
global using Sencilla.Component.Users;
global using Sencilla.Component.Audit;

using System.Linq;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Microsoft.Extensions.DependencyInjection;

public static class Bootstrap
{
    /// <summary>
    /// Wires the Sencilla audit change-log: the per-request <see cref="IAuditContext"/> and an
    /// <c>AuditHandler&lt;T&gt;</c> for every <see cref="IEntityAuditable"/> entity currently loaded
    /// (each handler writes through <c>ICreateRepository&lt;Audit&gt;</c>). Then chain
    /// <c>app.UseSencillaAudit()</c> (after <c>UseSencillaUser</c>) for the
    /// X-Audit-Reason header.
    ///
    /// Registration is EXPLICIT, not discovery-based: <c>AddSencilla()</c>'s <c>[AutoDiscovery]</c> scan runs
    /// once, up front, over the already-loaded assemblies — but this component (and the app's entity assemblies)
    /// are typically loaded lazily AFTER that scan, so a discovery-only wiring would register nothing and the
    /// audit log would be silently inert. Call this after the entity assemblies are loaded (i.e. after the app
    /// registers its own components) so every <see cref="IEntityAuditable"/> type is visible here. The audit
    /// entity's repositories come from <c>AddSencillaRepositoryForEF</c>, which must run after this.
    /// </summary>
    public static IServiceCollection AddSencillaAudit(this IServiceCollection services)
    {
        services.TryAddScoped<IAuditContext, AuditContext>();

        var registrator = new AuditRegistrator();
        foreach (var type in LoadedTypes())
            if (type is { IsClass: true, IsAbstract: false, IsGenericTypeDefinition: false }
                && typeof(IEntityAuditable).IsAssignableFrom(type))
                registrator.Register(services, type);

        return services;
    }

    static IEnumerable<Type> LoadedTypes() => AppDomain.CurrentDomain
        .GetAssemblies()
        .SelectMany(a => { try { return a.GetTypes(); } catch (ReflectionTypeLoadException e) { return e.Types.Where(t => t != null)!; } });
}
