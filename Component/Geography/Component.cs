global using Sencilla.Core;

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
