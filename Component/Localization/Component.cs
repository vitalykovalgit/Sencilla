
using Sencilla.Core.Component;
using Sencilla.Core.Injection;

namespace Sencilla.Component.Localization
{
    public class LocalizationComponent : IComponent
    {
        public string Type => nameof(LocalizationComponent);

        public void Init(IResolver resolver)
        {
            
        }
    }
}
