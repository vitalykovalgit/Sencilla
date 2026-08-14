
global using Microsoft.AspNetCore.Http;
global using Microsoft.AspNetCore.Mvc;
global using Microsoft.Extensions.DependencyInjection;
global using Microsoft.Extensions.DependencyInjection.Extensions;
global using Microsoft.Extensions.Options;
global using Microsoft.Extensions.Primitives;
global using Sencilla.Component.Files;
global using Sencilla.Core;
global using Sencilla.Web;

global using System.Collections.Immutable;
global using System.ComponentModel.DataAnnotations;
global using System.ComponentModel.DataAnnotations.Schema;
global using System.Net.Mime;
global using System.Text;
global using System.Text.RegularExpressions;

using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;

[assembly: AutoDiscovery]

namespace Microsoft.Extensions.DependencyInjection;

public static class Bootstrap
{
    public static IServiceCollection AddSencillaFiles(this IServiceCollection services, Action<SencillaFilesOptions>? configure = null)
    {
        var options = new SencillaFilesOptions(services);
        configure?.Invoke(options);
        options.Done();

        services.TryAddSingleton(options);

        services.TryAddKeyedTransient<IFileRequestHandler, CreateFileHandler>(IFileRequestHandler.ServiceKey(CreateFileHandler.Method));
        services.TryAddKeyedTransient<IFileRequestHandler, UploadFileHandler>(IFileRequestHandler.ServiceKey(UploadFileHandler.Method));
        services.TryAddKeyedTransient<IFileRequestHandler, HeadFileHandler>(IFileRequestHandler.ServiceKey(HeadFileHandler.Method));
        services.TryAddKeyedTransient<IFileRequestHandler, DeleteFileHandler>(IFileRequestHandler.ServiceKey(DeleteFileHandler.Method));

        services.TryAddTransient<IFilePathResolver, FilePathResolver>();
        return services;
    }

    /// <param name="bindOwnSection">
    /// Bind <c>SencillaFiles:{Section}</c> over <paramref name="options"/>. False for a NAMED
    /// instance (<c>SencillaFiles:Storages:{name}</c>), which the caller has already bound —
    /// rebinding the shared flat section would overwrite it.
    /// </param>
    public static SencillaFilesOptions AddStorageInternal<TStorage, TOptions>(this SencillaFilesOptions root,
        TOptions options, IConfiguration? configuration = null, IConfigurationSection? section = null,
        bool bindOwnSection = true)
        where TStorage: class, IFileStorage
        where TOptions: BaseFilesOptions
    {

        var dirs = new Dictionary<string, string>();
        if (configuration is not null)
        {
            if (bindOwnSection)
                configuration.GetSection($"{root.Section}:{options.Section}").Bind(options);
            configuration.GetSection($"{root.Section}:Dirs").Bind(dirs);
        }

        if (section is not null)
        {
            if (bindOwnSection)
                section.GetSection($"{root.Section}:{options.Section}").Bind(options);
            section.GetSection($"{root.Section}:Dirs").Bind(dirs);
        }

        // merge directories 
        foreach (var kvp in root.Dirs)
            options.Dirs.TryAdd(kvp.Key, kvp.Value);

        if (dirs != null)
        {
            foreach (var kvp in dirs)
                options.Dirs.TryAdd(kvp.Key, kvp.Value);
        }

        // The id is what File.Storage records, so two storages sharing one is not a duplicate
        // registration to skip quietly — it is a bucket nobody can address, and files written
        // under that id would come back from the wrong storage. TryAdd would drop the second
        // silently; say so instead.
        if (root.Services.Any(d => d.IsKeyedService && d.ServiceType == typeof(IFileStorage) && Equals(d.ServiceKey, options.Type)))
            throw new InvalidOperationException(
                $"Storage id {options.Type} is already registered. Give each storage its own Type " +
                $"in configuration — it is persisted in File.Storage and must stay unique.");

        // Each registration keeps its OWN options object. Two instances of one provider (say two
        // S3 buckets) differ only by configuration, so letting DI resolve TOptions would hand
        // both the same singleton and silently collapse them into one storage.
        root.Services.TryAddKeyedTransient<IFileStorage>(options.Type,
            (sp, _) => ActivatorUtilities.CreateInstance<TStorage>(sp, options));

        // Kept for consumers that inject the concrete options type directly. With several
        // instances of one provider the first registered wins here — resolve by key instead.
        root.Services.TryAddSingleton(options);

        if (options.UseAsDefault)
            root.Services.TryAddTransient<IFileStorage>(sp => ActivatorUtilities.CreateInstance<TStorage>(sp, options));

        return root;
    }

    /// <summary>
    /// ETag + immutable freshness for the file stream endpoint, bound from
    /// <c>SencillaFiles:StreamCache</c>. Pipeline side: <see cref="UseSencillaFiles"/>.
    /// </summary>
    public static SencillaFilesOptions AddStreamCache(this SencillaFilesOptions root, IConfiguration configuration)
    {
        root.Services.Configure<FileStreamCacheOptions>(configuration.GetSection($"{root.Section}:{FileStreamCacheOptions.Section}"));
        return root;
    }

    /// <summary>
    /// The component's request pipeline — the stream cache for now. Place BEFORE UseEndpoints so the
    /// 304 short-circuit can skip the blob read.
    /// </summary>
    public static IApplicationBuilder UseSencillaFiles(this IApplicationBuilder app)
        => app.UseMiddleware<FileStreamCacheMiddleware>();

    public static IApplicationBuilder UseTusResumableUpload(this IApplicationBuilder builder, Action<FileUploadOptions> configure)
    {
        var options = new FileUploadOptions { Route = string.Empty };
        configure(options);
        return builder.UseMiddleware<UploadFileMiddleware>(options);
    }

    public static IApplicationBuilder UseTusResumableUpload(this IApplicationBuilder builder, string route)
    {
        return builder.UseTusResumableUpload(o => o.Route = route);
    }
}
