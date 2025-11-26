
global using Microsoft.AspNetCore.Http;
global using Microsoft.AspNetCore.Mvc;
global using Microsoft.Extensions.DependencyInjection;
global using Microsoft.Extensions.DependencyInjection.Extensions;
global using Sencilla.Component.Files;
global using Sencilla.Core;
global using Sencilla.Web;
global using System.Collections.Immutable;
global using System.ComponentModel.DataAnnotations;
global using System.ComponentModel.DataAnnotations.Schema;
global using System.Net.Mime;
global using System.Text;
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

        services.TryAddTransient<IFilePathResolver, FilePathResolver>();
        return services;
    }

    public static SencillaFilesOptions AddStorageInternal<TStorage, TOptions>(this SencillaFilesOptions root, 
        TOptions options, IConfiguration? configuration = null, IConfigurationSection? section = null)
        where TStorage: class, IFileStorage
        where TOptions: BaseFilesOptions
    {

        var dirs = new Dictionary<string, string>();
        if (configuration is not null)
        {
            configuration.GetSection($"{root.Section}:{options.Section}").Bind(options);
            configuration.GetSection($"{root.Section}:Dirs").Bind(dirs);
        }

        if (section is not null)
        {
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

        root.Services.TryAddKeyedTransient<IFileStorage, TStorage>(options.Type);
        root.Services.TryAddSingleton(options);

        if (options.UseAsDefault)
            root.Services.TryAddTransient<IFileStorage, TStorage>();

        return root;
    }

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
