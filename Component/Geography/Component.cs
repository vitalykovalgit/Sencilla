global using Sencilla.Core;
global using Sencilla.Web;

[assembly: AutoDiscovery]

namespace Microsoft.Extensions.DependencyInjection
{
    public class GeographyComponent : IComponent
    {
        public string Type => nameof(GeographyComponent);

        public void Init(IContainer container)
        {
        }
    }
}
