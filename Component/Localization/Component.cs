
using Sencilla.Core;

namespace Sencilla.Component.Localization
{
    public class LocalizationComponent : IComponent
    {
        public string Type => nameof(LocalizationComponent);

        public void Init(IRegistrator container)
        {
        }
    }
}
