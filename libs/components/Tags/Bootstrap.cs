global using System.Globalization;
global using System.Reflection;
global using System.Linq.Expressions;
global using System.ComponentModel.DataAnnotations.Schema;

global using Microsoft.Extensions.DependencyInjection;
global using Microsoft.Extensions.DependencyInjection.Extensions;

global using Microsoft.AspNetCore.Mvc;

global using Sencilla.Core;
global using Sencilla.Web;
global using Sencilla.Component.Tags;
global using Sencilla.Repository.EntityFramework;

global using Microsoft.EntityFrameworkCore;
global using Microsoft.EntityFrameworkCore.Metadata.Builders;

using System.Linq;

// Discovered like every other component: AddSencilla()'s scan force-loads the full assembly graph first, so
// RepositoryRegistrator sees EntityTag and every {Entity}Tag link entity (repositories + DynamicDbContext
// model), and TagRegistrator can resolve a linked entity's link type by scanning loaded types.
[assembly: AutoDiscovery]

namespace Microsoft.Extensions.DependencyInjection;

public static class Bootstrap
{
    /// <summary>
    /// Wires generic entity tagging: one <c>ITagRepository</c> per <see cref="IEntityTaggable"/> entity — inline
    /// column, per-entity link table, or the shared <c>tag.EntityTag</c> table, whichever the entity declares —
    /// plus the read-pipeline handlers that make <c>?tag=</c> filtering and <c>tags: string[]</c> hydration work
    /// for it.
    ///
    /// <para>The <c>[assembly: AutoDiscovery]</c> scan already registers all of this when <c>AddSencilla()</c>
    /// runs; the loop below only matters for hosts that skip the scan.
    /// <see cref="Sencilla.Component.Tags.TagRegistrator"/> is idempotent, so both paths may run.</para>
    ///
    /// <para>Storage requiring DDL: an <see cref="IEntityTaggableShared"/> entity needs the
    /// <c>Sencilla.Component.Tags.Mssql</c> package referenced by the consuming database project; an
    /// <see cref="IEntityTaggableLinked"/> entity needs its own <c>{Entity}Tag</c> table; an inline one needs a
    /// <c>[Tags] NVARCHAR(4000) NULL</c> column on its own table.</para>
    /// </summary>
    public static IServiceCollection AddSencillaTags(this IServiceCollection services)
    {
        // How DynamicDbContext learns to map (or ignore) a tag column without knowing this component exists.
        // TryAddEnumerable so the scan's own registration and this one cannot both land.
        services.TryAddEnumerable(ServiceDescriptor.Transient<IEntityModelConfigurator, TagsModelConfigurator>());

        var registrator = new TagRegistrator();
        foreach (var type in LoadedTypes())
            if (type is { IsClass: true, IsAbstract: false, IsGenericTypeDefinition: false }
                && typeof(IEntityTaggable).IsAssignableFrom(type))
                registrator.Register(services, type);

        return services;
    }

    static IEnumerable<Type> LoadedTypes() => AppDomain.CurrentDomain
        .GetAssemblies()
        .SelectMany(a => { try { return a.GetTypes(); } catch (ReflectionTypeLoadException e) { return e.Types.Where(t => t != null).Select(t => t!); } });
}
