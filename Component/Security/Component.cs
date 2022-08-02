
using Sencilla.Core;

namespace Microsoft.Extensions.DependencyInjection
{
    public class SecurityComponent : IComponent
    {
        public string Type => nameof(SecurityComponent);

        public void Init(IContainer container)
        {
        }
    }
}
