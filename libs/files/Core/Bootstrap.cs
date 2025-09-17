
global using System.Text;
global using System.Net.Mime;
global using System.Collections.Immutable;
global using System.ComponentModel.DataAnnotations;
global using System.ComponentModel.DataAnnotations.Schema;

global using Microsoft.AspNetCore.Http;
global using Microsoft.AspNetCore.Mvc;
global using Microsoft.Extensions.DependencyInjection;
global using Microsoft.Extensions.DependencyInjection.Extensions;

global using Sencilla.Core;
global using Sencilla.Web;
global using Sencilla.Component.Files;

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

        services.TryAddKeyedTransient<ITusRequestHandler, CreateFileHandler>(ITusRequestHandler.ServiceKey(CreateFileHandler.Method));
        services.TryAddKeyedTransient<ITusRequestHandler, UploadFileHandler>(ITusRequestHandler.ServiceKey(UploadFileHandler.Method));
        services.TryAddKeyedTransient<ITusRequestHandler, HeadFileHandler>(ITusRequestHandler.ServiceKey(HeadFileHandler.Method));

        services.TryAddTransient<IFileRepository, DbFileRepository>();
        services.TryAddTransient<IFileUploadRepository, DbFileUploadRepository>();

        return services;
    }
}
