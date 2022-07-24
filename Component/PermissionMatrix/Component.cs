
using Sencilla.Core;

namespace Sencilla.Component.Security
{
    public class SecurityComponent : IComponent
    {
        public string Type => nameof(SecurityComponent);

        public void Init(IRegistrator container)
        {
        }
    }
}
