global using System.Reflection;
global using System.Text.Json;
global using System.ComponentModel.DataAnnotations.Schema;

global using Microsoft.AspNetCore.Http;
global using Microsoft.AspNetCore.Builder;
global using Microsoft.Extensions.DependencyInjection;

global using Sencilla.Core;
global using Sencilla.Component.Users;
global using Sencilla.Component.Security;
global using Sencilla.Component.Audit;

using System.Linq;
using Microsoft.Extensions.DependencyInjection.Extensions;

// Discovered like every other component: AddSencilla()'s scan force-loads the full assembly
// graph first, so RepositoryRegistrator sees the Audit entity (repositories + DynamicDbContext
// model) and AuditRegistrator wires AuditHandler<T> for every IEntityAuditable type.
[assembly: AutoDiscovery]

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
    /// The <c>[assembly: AutoDiscovery]</c> scan already registers the handlers when
    /// <c>AddSencilla()</c> runs; the loop below only matters for hosts that skip the scan.
    /// <see cref="Sencilla.Component.Audit.AuditRegistrator"/> is idempotent, so both paths may run.
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
