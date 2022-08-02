using Sencilla.Core;

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
