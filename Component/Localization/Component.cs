using Sencilla.Core;

[assembly: AutoDiscovery]

namespace Microsoft.Extensions.DependencyInjection
{
    public class LocalizationComponent : IComponent
    {
        public string Type => nameof(LocalizationComponent);

        public void Init(IContainer container)
        {
        }
    }
}
