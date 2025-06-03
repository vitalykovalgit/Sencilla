global using Sencilla.Core;
global using Sencilla.Component.Files;
global using Sencilla.Component.FilesTus;

global using System.Text;
global using System.Net.Mime;
global using System.Collections.Immutable;

global using Microsoft.AspNetCore.Http;
global using Microsoft.AspNetCore.Builder;
global using Microsoft.Extensions.DependencyInjection;

[assembly: AutoDiscovery]

namespace Microsoft.Extensions.DependencyInjection;

public class FilesTusComponent : IComponent
{
    public string Type => "Sencilla.Component.FilesTus";

    public void Init(IContainer container)
    {
        container.RegisterType<ITusRequestHandler, CreateFileHandler>(ITusRequestHandler.ServiceKey(CreateFileHandler.Method));
        container.RegisterType<ITusRequestHandler, UploadFileHandler>(ITusRequestHandler.ServiceKey(UploadFileHandler.Method));
        container.RegisterType<ITusRequestHandler, HeadFileHandler>(ITusRequestHandler.ServiceKey(HeadFileHandler.Method));
    }
}
